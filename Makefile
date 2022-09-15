CONFIGURATION=Debug
APP_PROJECT=Dafda.Gendis.App/Dafda.Gendis.App.csproj
OUTPUT_DIR=${PWD}/.output
OUTPUT_DIR_APP=$(OUTPUT_DIR)/app
OUTPUT_DIR_TESTRESULTS=$(OUTPUT_DIR)/testresults
APP_IMAGE_NAME=dafda.gendis

init: clean restore build unittests

clean:
	@-rm -Rf $(OUTPUT_DIR)
	@mkdir $(OUTPUT_DIR)
	@mkdir $(OUTPUT_DIR_APP)
	@cd src && dotnet clean \
		--configuration $(CONFIGURATION) \
		--nologo \
		-v q \
		$(APP_PROJECT)

restore:
	@cd src && dotnet restore -v q

build:
	@cd src && dotnet build \
		--configuration $(CONFIGURATION) \
		--no-restore \
		--nologo \
		-v q

unittests:
	@cd src && dotnet test \
		--configuration $(CONFIGURATION) \
		--no-restore \
		--no-build \
		-r $(OUTPUT_DIR_TESTRESULTS) \
		--logger "trx;LogFileName=testresults.trx" \
		--filter Category!=Integration \
		--collect "XPlat Code Coverage" \
		--nologo \
		-v q

publish:
	@cd src && dotnet publish \
		--configuration $(CONFIGURATION) \
		--output $(OUTPUT_DIR_APP) \
		--no-build \
		--no-restore \
		--nologo \
		-v q \
		$(APP_PROJECT)

appcontainer:
	@docker build -t ${APP_IMAGE_NAME} .

ci: CONFIGURATION=Release
ci: clean restore build unittests publish appcontainer

dev:
	cd src && dotnet watch --no-hot-reload --project $(APP_PROJECT) run