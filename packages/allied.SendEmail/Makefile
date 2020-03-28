.ONESHELL:
#HANDLER = $(jq -r '.["function-handler"]' "src/allied.SendEmail/aws-lambda-tools-defaults.json")
#$(jq -r 'with_entries(if .key | contains("-") then .key |= sub("-";"_") else . end)' "src/allied.SendEmail/aws-lambda-tools-defaults.json")
	
#BUCKET=allied-sendemail-bucket-rbii3pjp7dfn
BUCKET=allied-sendemail-alliedsendemailbucket-s1374jczqqik
BUCKET=allied-sendemail-alliedsendemailbucket-1jtc33v6ihxvn
BUCKET=allied-sendemail-alliedsendemailbucket-1cvgkx6fqvfeq
BUCKET_DEPLOY=aws-sam-cli-managed-default-samclisourcebucket-16vmd993fifjh
BUCKET=allied-sendemail-alliedsendemailbucket-8o4xar2vyhwi
PROFILE=${AWS_PROFILE}
STACKNAME=allied-SendEmail

BUCKET_DEPLOY=deployments-allied
BUCKET=allied-email-bucket
BUCKET=allied-ftp-to-s3

all: clean config
	echo "all"

build:
	cd "src/allied.SendEmail"
	dotnet build -c Release

package:
	cd "src/allied.SendEmail"
	dotnet lambda package


clean:
	rm -rf .aws-sam
	cd "src/allied.SendEmail"
	dotnet clean
	
config:
	PROFILE=${AWS_PROFILE} j2 -f env src/allied.SendEmail/aws-lambda-tools-defaults.json.j2 > src/allied.SendEmail/aws-lambda-tools-defaults.json
	echo "# GENERATED FILE - DO NOT EDIT" > sam-template.yaml
	j2 -f json sam-template.j2.yaml src/allied.SendEmail/aws-lambda-tools-defaults.json >> sam-template.yaml
	yamllint -d relaxed sam-template.yaml
	sam validate -t sam-template.yaml

sam: config
	yes | sam deploy --template-file sam-template.yaml --debug --s3-bucket ${BUCKET_DEPLOY} --stack-name $(STACKNAME) --tags janitor.optout=true --parameters

deploy: config
	cd "src/allied.SendEmail"
	dotnet lambda deploy-function --tags janitor.optout=true 

email:
	aws s3 rm "s3://${BUCKET}/HORNER-TEST.csv"
	aws s3 cp "HORNER-TEST.csv" s3://${BUCKET}/

size:
	aws s3api head-object --bucket ${BUCKET} --key "HORNER-TEST.csv"

json:
	aws s3 cp "src/allied.SendEmail/aws-lambda-tools-defaults.json" s3://${BUCKET}/

template:
	aws s3 cp "src/allied.SendEmail/template.tpl" s3://${BUCKET}/.config/ --profile=$(PROFILE)

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
	aws cloudformation delete-stack --stack-name $(STACKNAME)


s3set:
	aws s3api put-bucket-tagging --bucket ${BUCKET} --tagging TagSet=[{Key=SendEmail:To,Value=david.horner@alliedpayment.com}]

s3tags:
	aws s3api get-bucket-tagging --bucket ${BUCKET}

rb:
	aws s3 ls | cut -d" " -f 3 | grep allied-sendemail | xargs -I{} aws s3 rb s3://{}

empty:
	aws s3 ls | cut -d" " -f 3 | grep allied-sendemail | xargs -I{} aws s3 rb s3://{} --force
