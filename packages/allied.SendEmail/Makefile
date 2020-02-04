.ONESHELL:
	
BUCKET=allied-email-bucket

deploy:
	cd "src/allied.SendEmail"
	dotnet lambda deploy-function

email:
	aws s3 cp "src/allied.SendEmail/template.tpl" s3://${BUCKET}/

template:
	aws s3 cp "src/allied.SendEmail/template.tpl" s3://${BUCKET}/.allied/

ubu_deps:
	sudo apt-get update
	sudo apt-get install apt-transport-https
	sudo apt-get update
	sudo apt-get install dotnet-sdk-3.1
	dotnet tool install -g Amazon.Lambda.Tools
