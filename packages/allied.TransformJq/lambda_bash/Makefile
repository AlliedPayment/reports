
BUCKET=allied-deploy
AWS_DEFAULT_REGION=us-east-1
# setx AWS_DEFAULT_REGION us-east-1


update:
	./lambda_bash.sh -o update -s transformJq.sh

run:
	cat example.json | ./lambda_bash.sh -o run -s transformJq.sh

config:
	aws s3 cp transformJqConfig.json s3://$(BUCKET)/.allied/transformJqConfig.json

example:
	aws s3 cp example.json s3://$(BUCKET)/

jqdefs:
	aws s3 cp jqdefs.jq s3://$(BUCKET)/.allied/jqdefs.jq


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
	yes | pip install awscli aws-shell --upgrade # --user

help:
	cat Makefile | less
