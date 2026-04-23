#!/bin/bash
set -euo pipefail

# Define the image name
IMAGE_NAME="evo-api"

# Create a local results directory if it doesn't exist
mkdir -p results

echo "--- Building the Docker Image... ---"
DOCKER_BUILDKIT=1 docker build -t $IMAGE_NAME .

echo "--- Starting the FastAPI Server... ---"
echo "Unity should connect to http://localhost:8000"
echo "Data logs will be saved to the local ./results folder."

docker run --rm -v "$(pwd)/results:/app/results" -p 8000:8000 $IMAGE_NAME
