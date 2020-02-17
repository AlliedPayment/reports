# allied.SendEmail

This lambda function processes s3 bucket events.  It will generate an email with the evented file attached.


Emails are defined via scriban templates.  Variables can be set and replaced in templates.


Currently using SMTP to outside; could be using amazon SES (simple email service)
https://github.com/simalexan/api-lambda-send-email-ses

