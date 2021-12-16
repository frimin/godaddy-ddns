all: build

.PHONY: build

docker:
	docker build -t godaddy-ddns .

build:
	dotnet publish godaddy-ddns -c Release -o build

clean:
	rm -rf ./build
	rm -rf ./app/bin
	rm -rf ./app/obj
