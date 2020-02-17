.ONESHELL:
#HANDLER = $(jq -r '.["function-handler"]' "src/allied.SendEmail/aws-lambda-tools-defaults.json")
#$(jq -r 'with_entries(if .key | contains("-") then .key |= sub("-";"_") else . end)' "src/allied.SendEmail/aws-lambda-tools-defaults.json")
	
BUCKET=allied-email-bucket
#BUCKET=allied-sendemail-bucket-rbii3pjp7dfn
BUCKET=allied-sendemail-alliedsendemailbucket-s1374jczqqik
BUCKET=allied-sendemail-alliedsendemailbucket-1jtc33v6ihxvn
BUCKET=allied-sendemail-alliedsendemailbucket-1cvgkx6fqvfeq
BUCKET=allied-sendemail-alliedsendemailbucket-8o4xar2vyhwi

clean:
	rm -rf .aws-sam
	cd "src/allied.SendEmail"
	dotnet clean
	
config:
	j2 -f json template.yaml.j2 src/allied.SendEmail/aws-lambda-tools-defaults.json > template.yaml
	yamllint -d relaxed template.yaml
	sam validate -t template.yaml

sam: config
	yes | sam deploy

deploy:
	cd "src/allied.SendEmail"
	dotnet lambda deploy-function

email:
	aws s3 rm "s3://${BUCKET}/src/allied.SendEmail/template.tpl"
	aws s3 rm "s3://${BUCKET}/Makefile"
	aws s3 cp "src/allied.SendEmail/template.tpl" s3://${BUCKET}/
	aws s3 cp "Makefile" s3://${BUCKET}/

json:
	aws s3 cp "src/allied.SendEmail/aws-lambda-tools-defaults.json" s3://${BUCKET}/

template:
	aws s3 cp "src/allied.SendEmail/template.tpl" s3://${BUCKET}/.allied/

win_deps:
	choco install -y aws-vault python3 golang
	py -3 -m pip install --upgrade pip
	pip3 install j2cli
	pip3 install aws-sam-cli
	pip3 install yamllint

ubu_deps:
	sudo apt-get update
	sudo apt-get install apt-transport-https
	sudo apt-get update
	sudo apt-get install dotnet-sdk-3.1
	dotnet tool install -g Amazon.Lambda.Tools

destroy:
	aws cloudformation delete-stack --stack-name allied-SendEmail

s3tags:
	aws s3api get-bucket-tagging --bucket ${BUCKET}

rb:
	aws s3 ls | cut -d" " -f 3 | grep allied-sendemail | xargs -I{} aws s3 rb s3://{}

empty:
	aws s3 ls | cut -d" " -f 3 | grep allied-sendemail | xargs -I{} aws s3 rb s3://{} --force
