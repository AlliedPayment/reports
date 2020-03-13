.ONESHELL:

FUNCNAME=transformJq
BUCKET=allied-transformjq
AWS_DEFAULT_REGION=us-east-1
# setx AWS_DEFAULT_REGION us-east-1
export PATH:=$(PATH):~/.local/bin

update:
	./lambda_bash.sh -o update -s transformJq.sh

delete:
	aws lambda delete-function --function-name $(FUNCNAME)

version:
	aws lambda publish-version --function-name $(FUNCNAME)

alias:
	aws lambda create-alias \
    	--function-name $(FUNCNAME) \
    	--description "alias for live version of $(FUNCNAME)" \
    	--function-version 1 \
    	--name LIVE

events3:
	~/.local/bin/j2 -f json s3-eventing.json.j2 > s3-eventing.json
	aws s3api put-bucket-notification-configuration --bucket $(BUCKET)  --notification-configuration file://./s3-eventing.json

run:
	cat example.json | ./lambda_bash.sh -o run -s transformJq.sh

config:
	aws s3 cp transformJqConfig.json s3://$(BUCKET)/.allied/transformJqConfig.json

example:
	aws s3 cp example.json s3://$(BUCKET)/

jqdefs:
	aws s3 cp jqdefs.jq s3://$(BUCKET)/.allied/jqdefs.jq

mb:
	aws s3 mb s3://$(BUCKET)

deploy:
	./lambda_bash.sh -o deploy -s transformJq.sh

# generates README.md (req! choco install ruby && gem install markdown_helper)
readme:
	markdown_helper include --pristine README.t.md README.md 

# generate an AUTHORS shortlog
authors:
	git shortlog -se  > AUTHORS

ubu_deps:
	sudo apt install make jq python-minimal python-pip -y
	#export PATH=$HOME/.local/bin:$PATH
	yes | sudo -H pip install awscli --upgrade --ignore-installed
	pip3 install j2cli

help:
	cat Makefile | less
