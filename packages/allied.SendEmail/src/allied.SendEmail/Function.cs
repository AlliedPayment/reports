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

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace allied.SendEmail
{
    public class Function
    {
        IAmazonS3 S3Client { get; set; }

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

        //public async task<string> functionhandler(string path, ilambdacontext context)
        //{
        //    return "dude! ${path}";
        //}

        /// <summary>
            /// This method is called for every Lambda invocation. This method takes in an S3 event object and can be used 
            /// to respond to S3 notifications.
            /// </summary>
            /// <param name="evnt"></param>
            /// <param name="context"></param>
            /// <returns></returns>
            public async Task<string> FunctionHandler(S3Event evnt, ILambdaContext context)
        {
            var s3Event = evnt.Records?[0].S3;
            if (s3Event == null)
            {
                return "this function only handles s3 events.";
            }

            try
            {
                
                    var svc = new MailService();
                    dynamic email = new ExpandoObject();

                    email.subject = "dudeman";
                    email.from = "noreply@alliedpayment.com";
                    email.reply = email.from;
                    email.to = "david.horner@alliedpayment.com";
                    email.body = $"{DateTime.UtcNow.ToString()} the bucket { s3Event.Bucket.Name} created { s3Event.Object.Key}";
                    var template = "";
                    string  tFile=null;
                    GetObjectMetadataResponse response;

                    try
                    {
                        response =
                            await this.S3Client.GetObjectMetadataAsync(s3Event.Bucket.Name, s3Event.Object.Key);
                    }
                    catch (Exception e)
                    {
                        return $"exception man! {s3Event.Bucket.Name}, {s3Event.Object.Key}" + e.ToString();
                    }


                    context.Logger.LogLine(
                        "getting tags");
                    var tags =await this.S3Client.GetBucketTaggingAsync(new GetBucketTaggingRequest() {BucketName = s3Event.Bucket.Name });
                    foreach(var t in tags.TagSet)
                    {
                        if (t.Key.Equals("allied:SendEmail:Subject", StringComparison.OrdinalIgnoreCase))
                        {
                            email.subject = t.Value;
                        } else if (t.Key.Equals("allied:SendEmail:Body", StringComparison.OrdinalIgnoreCase))
                        {
                            email.body = t.Value;
                        } else if (t.Key.Equals("allied:SendEmail:Template", StringComparison.OrdinalIgnoreCase))
                        {
                            template = t.Value;
                        }
                    }

                    if (template.Length>0)
                    {
                        tFile = Path.Combine(Path.GetTempPath(), template);
                        try
                        {

                            await S3Client.DownloadToFilePathAsync(s3Event.Bucket.Name, template, tFile, null,
                                CancellationToken.None);
                        }
                        catch (Exception e)
                        {
                            context.Logger.LogLine(@"No template {template} found.");
                        }
                    }

                    var path = Path.Combine(Path.GetTempPath(), s3Event.Object.Key);
                    context.Logger.LogLine(
                        $"Attempting to download: {s3Event.Bucket.Name}, {s3Event.Object.Key} to {path}");
                        await S3Client.DownloadToFilePathAsync(s3Event.Bucket.Name, s3Event.Object.Key, path, null, CancellationToken.None);
                        var files = new List<Attachment>();
                    files.Add(new Attachment(path));



                    if (!(tFile is null))
                    {


                        
                    }

                    svc.SendEmail(email.to, email.to,
                        email.from, email.from,
                        email.reply, email.reply,
                        email.subject, email.body, true, files );
                    File.Delete(path);
                    if (!(tFile is null))
                    {
                        File.Delete(tFile);
                    }

                return $"Sent email to: {email.to} { s3Event.Bucket.Name}, { s3Event.Object.Key}";
            }
            catch (Exception e)
            {
                context.Logger.LogLine($"Error getting object {s3Event.Object.Key} from bucket {s3Event.Bucket.Name}. Make sure they exist and your bucket is in the same region as this function.");
                context.Logger.LogLine(e.Message);
                context.Logger.LogLine(e.StackTrace);
                throw;
            }
        }
    }
}
