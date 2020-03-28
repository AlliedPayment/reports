import boto3
import json
import os
import pysftp
import paramiko
import base64
from urllib.parse import urljoin

file_names = []
dir_names = []
un_name = []


def lambda_handler(event, context):
    session = boto3.Session()
    from collections import defaultdict
    nested_dict = lambda: defaultdict(nested_dict)
    config = nested_dict()
    #hostkey= "ssh-rsa 1024 62:1f:6a:d4:d5:a4:5a:c1:1b:8b:f9:ae:ab:e2:a2:da"
    config['ftp2.alliedpayment.com']['ssh-rsa 1024']= \
                b'qzLQmar+yD/rFeX6N8q/PZqEaWUv87ysrneFVWDdfbk='
    config['ftp2.alliedpayment.com']['password']= \
                '8\CVRcJJ;s+g'
    config['ftp2.alliedpayment.com']['username']= \
                'BillGoPrismNetwork'
    default_host='ftp2.alliedpayment.com'
    dbucket=os.environ.get('BUCKET_DEST');
    if not dbucket:
        dbucket="allied-ftp-to-s3"
    hostname,port = default_host,22
    username,password = "BillGoPrismNetwork","8\CVRcJJ;s+g"
    hostkey=config[hostname]['ssh-rsa 1024']

    s3 = boto3.resource('s3')

    cnopts = pysftp.CnOpts()
    cnopts.hostkeys = None   
    #if hostkey:
    ##    data=base64.b64decode(hostkey)
    #    key=paramiko.RSAKey(data=data)
    #    sshkey = paramiko.RSAKey(data=key)
    #    cnopts.hostkeys.add(hostname, "ssh-rsa", sshkey)
    localdir='/tmp'
    os.chdir(localdir)
    body= ''
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
                s3.meta.client.upload_file(localpath, dbucket,filename[1:],None)
                
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



def store_files_name(fname):
    file_names.append(fname) 

def store_dir_name(dirname):
    dir_names.append(dirname)

def store_other_file_types(name):
    un_name.append(name)
