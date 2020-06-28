dotnet ?= dotnet
mode ?= debug
autoproj ?= autoproj
autoproj_option ?= 

ifeq ($(OS),Windows_NT)
	/ := \\
	os := win
else
	/ := /
	os := default
endif

.PHONY: develop preview release exam test local_cover
develop:
	@echo "building(develop)..."
	@$(dotnet) build -nologo -c Develop GeminiLab.Core2.GetOpt.sln

preview:
	@echo "building(preview)..."
	@-$(autoproj) $(autoproj_option) -- build_type=preview
	@$(dotnet) build -nologo -c Preview GeminiLab.Core2.GetOpt.sln

release:
	@echo "building(release)..."
	@-$(autoproj) $(autoproj_option) -- build_type=release
	@$(dotnet) build -nologo -c Release GeminiLab.Core2.GetOpt.sln

exam:
	@$(dotnet) run -p Exam$(/)Exam.csproj

test:
	@$(dotnet) test -nologo -p:CollectCoverage=true -p:CoverletOutputFormat=opencover -p:Exclude=[xunit.*]* -v=normal --no-build XUnitTester$(/)XUnitTester.csproj

local_cover: develop test
	reportgenerator -reports:.$(/)XUnitTester$(/)coverage.opencover.xml -targetdir:report.ignore
