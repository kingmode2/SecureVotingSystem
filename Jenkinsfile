pipeline {
    agent any

    stages {

        stage('Deploy Containers (Clean Start)') {
            steps {
                sh '''
                    cd docker

                    # ONLY restart app services (NOT whole infra)
                    docker compose up -d backend frontend postgres pgadmin prometheus grafana
                '''
            }
        }

        stage('Wait for Backend') {
            steps {
                sh '''
                    echo "Waiting for backend..."

                    # safer container detection
                    CONTAINER=$(docker ps -qf "name=backend")

                    if [ -z "$CONTAINER" ]; then
                        echo "Backend container not found ❌"
                        exit 1
                    fi

                    for i in $(seq 1 30); do
                        if docker exec $CONTAINER curl -f http://localhost:5000/metrics; then
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

                    CONTAINER=$(docker ps -qf "name=backend")

                    if [ -z "$CONTAINER" ]; then
                        echo "Backend container not found ❌"
                        exit 1
                    fi

                    docker exec $CONTAINER curl -f http://localhost:5000/metrics
                '''
            }
        }
    }
}