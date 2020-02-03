# allied.TransformJq

Why hello!  This is a lambda transformation function which operates via JSON/REGEX/JQ. Primarily, this function operates on an s3 bucket defined by env.ALLIED_DEPLOY_BUCKET.  

this project was inspired by lambda_bash and bash-lambda-layer.

Written by:

David.Horner


## Files 

* lambda_bash.sh - this is the deployment tool that deploys, runs, updates, describes and destroys your lambda.
* assume_role_policy.json - this is the template for the IAM role that is created for the lambda. Should not need editing
* s3_event.json - this is the event configuration used if you specify -b (s3 bucket event). The LambdaFunctionArn parameter will be updated with the real lambda ARN on deployment
* s3_event_filter_example.json - an example version of the s3_event.json file that filters on things like prefix and suffix, incase you only want to trigger your lambda for objects in certain directory, or with a certain suffix. 


## Usage

## Requirements

* bash
* awscli 1.16+
* jq 1.5+
* an AWS account with API credentials

## Example usage

1. Set up the enviornment
```bash
export AWS_PROFILE=myawsprofile
export AWS_DEFAULT_REGION=us-east-1
```

2. Create a simple s3 bucket that we can use as the trigger
```bash
aws s3 mb s3://css-my-bucket
```

3. Deploy the example script (ex_script.sh) that will respond to s3 events in our bucket
```bash
./lambda_bash.sh -o deploy -s ex_script.sh -b css-my-bucket
```
> creating role ex_script_lambdarole<br>
> attaching IAM policy arn:aws:iam::aws:policy/AdministratorAccess to role ex_script_lambdarole<br>
> sleeping 20 seconds to allow role to attach<br>
> deploying function ex_script<br>
> updating s3 event config s3_event.json with FunctionArn: arn:aws:lambda:us-east-1:150337127586:function:ex_script<br>
> adding permission for s3 to invoke function ex_script<br>
> attaching bucket-notification to bucket css-my-bucket for lambda ex_script with config s3_event.json<br>

4. Invoke the script manually
```bash
./lambda_bash.sh -o run -s ex_script.sh
```
> invoking lambda ex_script<br>
> ---------START RESPONSE------------<br>
> START RequestId: 35f0319c-fdbd-11e8-bc93-cf3ea1e65766 Version: $LATEST<br>
> EVENT DATA: {}<br>
> <br>
> list of s3 buckets<br>
> 2018-12-12 03:19:07 css-my-bucket<br>
> <br> 
> listing s3 bucket null that triggered this lambda with null <br>
> <br>
> An error occurred (AccessDenied) when calling the ListObjectsV2 operation: Access Denied <br>
> END RequestId: 35f0319c-fdbd-11e8-bc93-cf3ea1e65766 <br>
> REPORT RequestId: 35f0319c-fdbd-11e8-bc93-cf3ea1e65766	Init Duration: 274.37 ms	Duration: 3342.23 ms	Billed Duration: 3700 ms 	Memory Size: 1024 MB	Max Memory Used: 86 MB	<br>
> ---------END RESPONSE------------<br>

5. Update the script to do whatever you want, then update the lambda code
```bash
vi ex_script.sh
./lambda_bash.sh -o update -s ex_script.sh
```
> Updating function ex_script code <br>

6. Tail the cloudwatch logs for the lambda in one shell and trigger the lambda in another
```bash
./lambda_bash.sh -o tail -s ex_script.sh
```

Then from another shell, copy a file to s3 to trigger the lambda

```bash
aws s3 cp README.md s3://css-my-bucket
```

Now look at the logs from the tail session.

7. Undeploy the lambda

```bash
./lambda_bash.sh -o destroy -s ex_script.sh
```
> deleting lambda ex_script<br>
> detaching POLICY_ARN arn:aws:iam::aws:policy/AdministratorAccess from Role ex_script_lambdarole<br>
> deleting ROLE: ex_script_lambdarole<br>



### (ex. of using markdown_helper for inclusions)
markdown_helper include --pristine README.t.md AUTHORS
-- Horner , out.
@[:markdown](AUTHORS)
