import boto3
import json
import os
#import paramiko
#import ftplib
import pysftp


# Global variables are reused across execution contexts (if available)
session = boto3.Session()


def lambda_handler(event, context):
    """

    ssh-rsa 1024 qzLQmar+yD/rFeX6N8q/PZqEaWUv87ysrneFVWDdfbk=
ssh-rsa 1024 62:1f:6a:d4:d5:a4:5a:c1:1b:8b:f9:ae:ab:e2:a2:da


        AWS Lambda handler
        Parameters
        ----------
        context: object, required
            Lambda Context runtime methods and attributes

        Attributes
        ----------

        context.aws_request_id: str
            Lambda request ID
        context.client_context: object
            Additional context when invoked through AWS Mobile SDK
        context.function_name: str
            Lambda function name
        context.function_version: str
            Function version identifier
        context.get_remaining_time_in_millis: function
            Time in milliseconds before function times out
        context.identity:
            Cognito identity provider context when invoked through AWS Mobile SDK
        context.invoked_function_arn: str
            Function ARN
        context.log_group_name: str
            Cloudwatch Log group name
        context.log_stream_name: str
            Cloudwatch Log stream name
        context.memory_limit_in_mb: int
            Function memory

            https://docs.aws.amazon.com/lambda/latest/dg/python-context-object.html

        event: dict, required
        
        Returns
        ------
        
    """

    message = get_message()

    return message

file_names = []
dir_names = []
un_name = []

def store_files_name(fname):
    file_names.append(fname) 

def store_dir_name(dirname):
    dir_names.append(dirname)

def store_other_file_types(name):
    un_name.append(name)

def get_message():
    s3 = boto3.resource('s3')
    dbucket=os.environ.get('BUCKET_DEST');
    if not dbucket:
        dbucket="allied-ftp-to-s3"
    extargs={'ServerSideEncryption': 'AES256'}
    hostname,port = "ftp2.alliedpayment.com",22
    username,password = "BillGoPrismNetwork","8\CVRcJJ;s+g"
    cnopts = pysftp.CnOpts()
    cnopts.hostkeys = None   
    localdir='/tmp'
    os.chdir(localdir)
    with pysftp.Connection(hostname, username=username, password=password, cnopts=cnopts) as sftp:
        with sftp.cd('/'):
            sftp.walktree("/",store_files_name,store_dir_name,store_other_file_types,recurse=True)
            print(file_names,dir_names,un_name)
            for filename in file_names:
                localpath=os.path.join(localdir + filename)
                print(os.path.join(localdir + filename))
                print(f'dave downloading file {localdir} {filename} to {localpath}\n')                
                sftp.get(filename,localpath,callback=None,preserve_mtime=True)
                s3path=os.path.join('s3://',dbucket, filename)
                print(f'uploading file { s3path }\n')
                s3.meta.client.upload_file(dbucket,filename,extargs)

    #paramiko.util.log_to_file("paramiko.log")
    #transport = paramiko.Transport((host,port))
    
    # Auth    
    #transport.connect(None,username,password)

    # Go!    # Connecting to FTP

    #try:
    #    ftp = ftplib.FTP(ip)
    #    ftp.login(username, password)
    #except:
    #    print("Error connecting to FTP")

    #sftp = paramiko.SFTPClient.from_transport(transport)

    #

    # Close
    #if sftp: sftp.close()
    #if transport: transport.close()

    
    return  {
                'statusCode': 200,
                'body': 'Hello from Lambda Layers!'
            }





