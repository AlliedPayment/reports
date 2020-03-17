# Allied's SendEmail Lambdas
responds to events on an Amazon S3 bucket.

 - deployable via dotnet tool or via samcli. (named function, lambda in app - versioned and aliased)
 - use's current email class implementation with little modification. (SES todo)
 - configuration pulled from tags on s3 bucket or within .allied/template.tpl.
 - built to accept SNS events from S3. (todo additional API)

* after deploying configure S3 bucket as an event source to trigger your Lambda function or schedule the trigger from an event from the lambda.

## src\allied.SendEmail\template.tpl
The src\allied.SendEmail\template.tpl is a scriban template which defines the content and other runtime parameters (To, From, Subject, etc).  Example template is supplied.

scriban templating language
https://github.com/lunet-io/scriban/blob/master/doc/language.md


## Makefile

Default action is to clean, generate config, and package the zip.
 - make deploy - push single function using dotnet tool
 - make sam - deploy SAM application. 
 - make config - will generate template.yaml
 - make email - deletes two files and copies two files to destination bucket. (generates 2 emails via triggers placed in env)
 - make template - copy src/allied.SendEmail/template.tpl to the .allied/ config folder.
 - make win_deps - install the deps required for windows development.
 - make ubu_deps - install the deps required for apt based linux distro.
 - make destroy - deletes SAM application by stackname.
 - additional little tools for tagging and buckets...
 
Deploy function to AWS Lambda
```
    cd "allied.SendEmail/src/allied.SendEmail"
    dotnet lambda deploy-function
```
fdf