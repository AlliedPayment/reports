# allied.TransformJq

This lambda function will transform a file within an s3 bucket.  You can setup this function to be run via s3 events or invoked directly specifying a 'key' and 'bucket' parameter.

allied.TransformJq will attempt to read a configuration file /.allied/transformJqConfig.json.  An example is provided below.
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
| setting | description |
|---------|-------------|
| zipIncludeSrcFile | when zipping results, include the file we are transforming unmodified in the zip file. |
| deleteFromSrcBucket | after transformations/zip/copies are done, delete the orginal file from the s3 bucket |
| zipOutput | zip the transformation results |
| copyToBucket | url of the s3 destination bucket; no trailing / |

### projects used in the making
bash-lambda-layer/
lambda_bash/
