pipeline {
    agent any

    stages {

        stage('Deploy Containers (Fast)') {
            steps {
                sh '''
                    echo "Starting deployment..."

                    # Go to correct folder safely
                    cd docker

                    # Pull + start only needed services (faster)
                    docker compose up -d backend frontend postgres pgadmin prometheus grafana
                '''
            }
        }

        stage('Wait for Backend') {
            steps {
                sh '''
                    echo "Waiting for backend..."

                    # IMPORTANT: backend is inside docker network, not localhost
                    for i in {1..30}; do
                        if docker exec $(docker ps -qf name=backend) curl -f http://localhost:5000/metrics; then
                            echo "Backend is ready ✔"
                            exit 0
                        fi
                        echo "Waiting..."
                        sleep 2
                    done

                    echo "Backend failed to start ❌"
                    exit 1
                '''
            }
        }

        stage('Health Check Backend') {
            steps {
                sh '''
                    echo "Running final health check..."

                    docker exec $(docker ps -qf name=backend) curl -f http://localhost:5000/metrics
                '''
            }
        }
    }
}