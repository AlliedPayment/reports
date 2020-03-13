import boto3
import json
import os
import pysftp
from urllib.parse import urljoin


session = boto3.Session()
file_names = []
dir_names = []
un_name = []

def lambda_handler(event, context):
    return get_message()


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
    extargs=None
    #{'ServerSideEncryption': 'AES256'}
    hostname,port = "ftp2.alliedpayment.com",22
    username,password = "BillGoPrismNetwork","8\CVRcJJ;s+g"
    cnopts = pysftp.CnOpts()
    cnopts.hostkeys = None   
    localdir='/tmp'
    os.chdir(localdir)
#    file_names = []
    body= 'Hello from Lambda Layers!'
    file_names.clear()
    with pysftp.Connection(hostname, username=username, password=password, cnopts=cnopts) as sftp:
        with sftp.cd('/'):
            sftp.walktree("/",store_files_name,store_dir_name,store_other_file_types,recurse=True)
            print(file_names,dir_names,un_name)
            for filename in file_names:
                localpath=os.path.join(localdir + filename)
                print(os.path.join(localdir + filename))
                print(f'downloading file {filename} to {localpath}\n')                
                sftp.get(filename,localpath,callback=None,preserve_mtime=True)
                #s3path=  + filename
                body=f'uploading file {localpath} {dbucket}\n'
                print(body)
                s3.meta.client.upload_file(localpath, dbucket,filename[1:],extargs)
                
                bucket = s3.Bucket(dbucket)
                objs = list(bucket.objects.filter(Prefix=filename[1:]))
                if len(objs) > 0 and objs[0].key == filename[1:]:
                    print(f"{filename[1:]} exists; removing from sftp")
                    sftp.remove(filename[1:])
                else:
                    return  {
                                'statusCode': 400,
                                'body': 'failed'
                            }
    return  {
                'statusCode': 200,
                'body': body
            }





