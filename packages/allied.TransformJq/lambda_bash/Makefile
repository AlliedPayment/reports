BUCKET=allied-deploy

update:
	./lambda_bash.sh -o update -s transformJq.sh

config:
	aws s3 cp transformJqConfig.json s3://$(BUCKET)/.allied/transformJqConfig.json

example:
	aws s3 cp example.json s3://$(BUCKET)/

deploy:
	./lambda_bash.sh -o deploy -s transformJq.sh

# generates README.md (req! choco install ruby && gem install markdown_helper)
readme:
	markdown_helper include --pristine README.t.md README.md 

# generate an AUTHORS shortlog
authors:
	git shortlog -se  > AUTHORS

