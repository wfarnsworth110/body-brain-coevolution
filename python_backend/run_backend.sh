#!/bin/bash

# Define the image name
IMAGE_NAME="evo-api"

echo "--- 🛠️  Building the Docker Image... ---"
DOCKER_BUILDKIT=1 docker build -t $IMAGE_NAME .

echo "--- 🚀 Starting the FastAPI Server... ---"
echo "Unity should connect to http://localhost:8000"

docker run --rm -p 8000:8000 $IMAGE_NAME