default: help

.PHONY: publish clean help
.DEFAULT_GOAL := help

help:
	@grep -E '^[a-zA-Z_-]+:.*?## .*$$' $(MAKEFILE_LIST) | sort | awk 'BEGIN {FS = ":.*?## "}; {printf "\033[36m%-30s\033[0m %s\n", $$1, $$2}'

publish: ## This publishes the project
	dotnet publish dstream-dotnet-test.csproj -c Release -o out

clean: ## This cleans the project
	dotnet clean
	rm -rf out