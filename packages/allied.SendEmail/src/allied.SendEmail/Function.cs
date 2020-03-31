using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;

using Amazon.Lambda.Core;
using Amazon.Lambda.S3Events;
using Amazon.Runtime.Internal.Transform;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Amazon.S3.Util;
using Scriban;
using Scriban.Runtime;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace allied.SendEmail
{
    public class Function
    {
        IAmazonS3 S3Client { get; set; }
        ILambdaContext gContext  { get; set; }

        /// <summary>
        /// Default constructor. This constructor is used by Lambda to construct the instance. When invoked in a Lambda environment
        /// the AWS credentials will come from the IAM role associated with the function and the AWS region will be set to the
        /// region the Lambda function is executed in.
        /// </summary>
        public Function()
        {
            S3Client = new AmazonS3Client();
        }

        /// <summary>
        /// Constructs an instance with a preconfigured S3 client. This can be used for testing the outside of the Lambda environment.
        /// </summary>
        /// <param name="s3Client"></param>
        public Function(IAmazonS3 s3Client)
        {
            this.S3Client = s3Client;
        }
    public enum logLevel
    {
        SILENT = 0,
        NORM = 1,
        DEBUG = 2,
        ERROR = 3
    }
    public string ld(string msg, logLevel lvl = logLevel.ERROR)
    {
        if(lvl>0) {
            gContext.Logger.LogLine(msg);
        }
        return msg;
    }

    public string le(string msg, Exception e, logLevel lvl = logLevel.ERROR)
    {
        return le(e.Message)+le(e.StackTrace);
    }
    public string le(Exception e, logLevel lvl = logLevel.ERROR)
    {
        return le(e.Message)+le(e.StackTrace);
    }

    public string le(string msg, logLevel lvl = logLevel.ERROR)
    {
        if(lvl>0) {
            gContext.Logger.LogLine(msg);
        }
        return msg;
    }
    public string ll(string msg, logLevel lvl = logLevel.NORM)
    {
        if(lvl>0) {
            gContext.Logger.LogLine(msg);
        }
        return msg;
    }

	public async Task<string> GetS3String(string bucket, string key)
	{
		var tpath = Path.Combine(Path.GetTempPath(), key);
        await S3Client.DownloadToFilePathAsync(bucket, key, tpath, null,
        CancellationToken.None);
        string ret = File.ReadAllText(tpath, System.Text.Encoding.ASCII);
        File.Delete(tpath);
		return ret;
	}

    /// <summary>
        /// This method is called for every Lambda invocation. This method takes in an S3 event object and can be used
        /// to respond to S3 notifications.
        /// </summary>
        /// <param name="evnt"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task<string> FunctionHandler(S3Event evnt, ILambdaContext context)
        {
        gContext=context;
        var s3Event = evnt.Records?[0].S3;
        if (s3Event == null)
        {
            return le("this function only handles s3 events.");
        }
        string bucket=s3Event.Bucket.Name;
        string key=s3Event.Object.Key;

        try
        {
            double numbytes = await GetFileBytes(bucket,key);
            string filesize=GetFileSize(numbytes);
            var svc = new MailService();
            dynamic email = new ExpandoObject();

            email.subject = $"{bucket} notification for {key} - {numbytes} ({filesize})";
            email.from = null;
            email.reply = email.from;
            email.to = null;
            email.body = $"{DateTime.UtcNow.ToString()} the bucket { bucket} created { key}";
            var template = ".config/template.tpl";
            ll(email.subject);

            long maxInclude=0;
            bool includeContents=false;
            bool allowToOverride = true;
            var tags =await this.S3Client.GetBucketTaggingAsync(new GetBucketTaggingRequest() {BucketName = bucket });
            foreach(var t in tags.TagSet)
            {
                if (t.Key.Equals("SendEmail:Subject", StringComparison.OrdinalIgnoreCase))
                {
                    email.subject = t.Value;
                } else if (t.Key.Equals("SendEmail:To", StringComparison.OrdinalIgnoreCase))
                {
                    email.to = t.Value;
                } else if (t.Key.Equals("SendEmail:Body", StringComparison.OrdinalIgnoreCase))
                {
                    email.body = t.Value;
                } else if (t.Key.Equals("SendEmail:IncludeContents", StringComparison.OrdinalIgnoreCase))
                {
                    includeContents = true;
                } else if (t.Key.Equals("SendEmail:AllowToOverride", StringComparison.OrdinalIgnoreCase))
                {
                    allowToOverride = false;
                } else if (t.Key.Equals("SendEmail:Template", StringComparison.OrdinalIgnoreCase))
                {
                    template = t.Value;
                }
            }

            if (template.Length>0)
            {
                try
                {
                    string templateData=await GetS3String(bucket, template);

                    var scriptObject1 = new ScriptObject();
                    scriptObject1.Add("Name", bucket);
                    scriptObject1.Add("BucketName", bucket);
                    scriptObject1.Add("s3Event", s3Event);
                    scriptObject1.Add("email", email);
                    scriptObject1.Add("Subject", email.subject);
                    scriptObject1.Add("To", email.to);
                    scriptObject1.Add("From", email.from);
                    scriptObject1.Add("Key", key);
                    scriptObject1.Add("Filesize", filesize);

                    if(includeContents) {
                        if(numbytes>0) {
                            string data=await GetS3String(bucket, key);
                            scriptObject1.Add("Contents", data);
                        }
                        ll($"including {bucket} : {key} : {numbytes} bytes ");
                    }

                    var tc = new TemplateContext();
                    tc.PushGlobal(scriptObject1);

                    var scriban = Template.Parse(templateData);
                    if (scriban.HasErrors)
                    {
                        foreach (var error in scriban.Messages)
                        {
                            le($"scriban error: {error.ToString()}");
                        }
                    }
                    else
                    {
                        var result = scriban.Render(tc);
                        email.body = result;
                    }

                    if (scriptObject1["Subject"] != email.subject)
                    {
                        email.subject=scriptObject1["Subject"].ToString().Trim();
                        ld("SubjectAfter:" + scriptObject1["Subject"]);
                    }
                    if (allowToOverride && scriptObject1["To"] != email.to)
                    {
                        email.to = scriptObject1["To"];
                    }
                    if (scriptObject1["From"] != email.from)
                    {
                        email.from = scriptObject1["From"];
                        email.reply = scriptObject1["From"];
                    }
                } catch (Exception e) {
                    le($"No template {template} found. "+ e.Message);
                }
            }

            if(!(email.to is null)) {
            if(!(email.from is null)) {
                var path = Path.Combine(Path.GetTempPath(), key);
                ld($"Attach download: {bucket}, {key} to {path} ({numbytes} - {filesize})");
                await S3Client.DownloadToFilePathAsync(bucket, key, path, null, CancellationToken.None);
                var files = new List<Attachment>();
                files.Add(new Attachment(path));
                ll($"sendemail {email.to}, {email.to},{email.from}, {email.from},{email.reply}, {email.reply},{email.subject}, email.body, true, {files}" );
                try {

                svc.SendEmail($"{email.to}", $"{email.to}",
                    $"{email.from}", $"{email.from}",
                    $"{email.reply}", $"{email.reply}",
                    $"{email.subject}", $"{email.body}", true, files );
                } catch (Exception e)
                {
                    le(e.Message);
                    le(e.StackTrace);
                }
                File.Delete(path);
                return ll($"Sent email to: {email.to} { bucket}, { key}");
            } else {

                le($"email from null, no emails sent.");
                return "";
            }
            } else {

                le($"email to null, no emails sent.");
                return "";
            }
        }
        catch (Exception e)
        {
            return le($"exception man! {bucket}, {key}",e);
            throw;
        }
        }
        // https://stackoverflow.com/questions/14488796/does-net-provide-an-easy-way-convert-bytes-to-kb-mb-gb-etc

        private string GetFileSize(double byteCount)
        {
            string size = "0 Bytes";
            if (byteCount >= 1073741824.0)
                size = String.Format("{0:##.##}", byteCount / 1073741824.0) + " GB";
            else if (byteCount >= 1048576.0)
                size = String.Format("{0:##.##}", byteCount / 1048576.0) + " MB";
            else if (byteCount >= 1024.0)
                size = String.Format("{0:##.##}", byteCount / 1024.0) + " KB";
            else if (byteCount > 0 && byteCount < 1024.0)
                size = byteCount.ToString() + " Bytes";

            return size;
        }

        private async Task<double> GetFileBytes(string bucket, string key)
        {
            //ListObjectsRequest request = new ListObjectsRequest();
            //request.BucketName = bucket;
            //request.Prefix = key;
            //ListObjectsResponse response = S3Client.ListObjects(request);
            //long totalSize = 0;
            //foreach (S3Object o in response.S3Objects)
            //{
            //    totalSize += o.Size;
            //}

            GetObjectMetadataResponse m;
            m=await this.S3Client.GetObjectMetadataAsync(bucket, key);
            return (double)m.ContentLength;
        }
    }
}
