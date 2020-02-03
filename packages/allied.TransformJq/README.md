# allied.TransformJq

This lambda function will transform a file within an s3 bucket and copy the results to another s3 bucket.  You can setup this function to be run via s3 events or invoked directly specifying a 'key' and 'bucket' parameter.


In allieddev, the function is deployed here:
https://console.aws.amazon.com/lambda/home?region=us-east-1#/functions/transformJq?tab=configuration

allied.TransformJq will attempt to read a configuration file /.allied/transformJqConfig.json.  This file defines a json object that contains a "transforms" object hash.  Each json object within the "transforms" defines a transformation, the key of the transform object is used to provide readers with a short name for each transformation.

An example config is provided below.
```
{
	"transforms": {
		"pretty-print": {
			"filterRegEx": ".*\\.json",
			"jqTransform": ".[]",
			"jqOutputName": "pretty-dude-[FILENAME].json"
		},
		"merge-two-json-to-csv": {
			"filterRegEx": ".*\\.json",
			"jqOutputName": "[FILENAME]-payments.csv",
			"jqTransform": ".[2][0].Payment * .[2][0].EbillingInfo  | map(.) | @csv"
		},
		"dave.xml": {
			"filterRegEx": "dave.xml",
			"jqTransform": "[2][0].Payment | with_entries(select(.) | to_entries | map(.key), map(.value) | @csv"
		}
	},
	"zipIncludeSrcFile": true,
	"deleteFromSrcBucket": 0,
	"zipOutput": true,
	"copyToBucket": "s3://allied-email-bucket"
}
```

### settings:
The settings file is protected within the .allied folder of the source s3 bucket.  The function will not run if a valid transformJqConfig.json is not found.  Currently the transformations are defined only in settings file; there is no way to define transformations via invoke parameter.

## transformation settings:
A transformation defined within the "transforms" object will only run if the regex defined in 'filterRegEx' matches the file being processed.  'jqOutputName' defines the output filename of the transformed data;
```
{
   "transforms": {
   	"shortname": {
		"filterRegEx": "regex to match on filename to apply this transformation.",
		"jqTransform": "the jq transformation you'd like to run on the file.",
		"jqOutputName": "save the results of the transformation in this file; [FILENAME] is replaced with the src filename."
	}
   }
}
```
Please note: the regex's are proper regex as provided by grep.  As such you will need to make sure to .* instead of just *; or escape special characters like . etc.

## top level settings:

```
{
	"transforms": { ... }
	"zipIncludeSrcFile": "when zipping results, include the file we are transforming unmodified in the zip file.",
	"deleteFromSrcBucket": "after transformations/zip/copies are done, delete the orginal file from the s3 bucket",
	"zipOutput": "zip the transformation results",
	"copyToBucket": "url of the s3 destination bucket; no trailing /"
}
```

### projects used in the making

| Project | Url | Description |
|---------|-----| -------- |
| bash-lambda-layer/ | https://github.com/gkrizek/bash-lambda-layer | Run Bash scripts in AWS Lambda via Layers (currently using the public layer arn:aws:lambda:<region>:744348701589:layer:bash:8; committing for future extension and configuration management. |
| lambda_bash/ | https://github.com/cloudshiftstrategies/lambda_bash | Provides a deployment script to create the function. |


### Notes on building

setx AWS_DEFAULT_REGION us-east-1
set  ALLIED_BUCKET      your-bucket

There is a Makefile included in lambda_bash;  this file provides the commands to deploy, update, and setup configuration.

| make target | Description |
|-------------|-------------|
| make deploy | will deploy the lambda function and create a lambda role. |
| make update | only update the transformJq script. |
| make config | copy the transformJqConfig.json file to the bucket. |
| make example | copy the example.json file to the bucket. |
