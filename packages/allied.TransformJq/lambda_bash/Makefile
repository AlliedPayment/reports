BUCKET=allied-deploy

update:
	./lambda_bash.sh -o update -s transformJq.sh

config:
	aws s3 cp transformJqConfig.json s3://$(BUCKET)/.allied/transformJqConfig.json

example:
	aws s3 cp example.json s3://$(BUCKET)/

deploy:
	./lambda_bash.sh -o deploy -s transformJq.sh
