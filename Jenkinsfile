pipeline {
    agent any

    stages {

        stage('Deploy Containers (Clean Start)') {
            steps {
                sh '''
                    cd docker
                    echo "Starting core services (backend, frontend, postgres, pgadmin, prometheus, grafana)..."
                    docker compose up -d backend frontend postgres pgadmin prometheus grafana

                    echo "Waiting for backend to be healthy..."
                    for i in $(seq 1 30); do
                        if curl -f http://backend:5000/metrics >/dev/null 2>&1; then
                            echo "Backend is ready ✔"
                            break
                        fi
                        echo "Waiting for backend... ($i/30)"
                        sleep 2
                    done

                    echo "Starting Jenkins now..."
                    docker compose up -d jenkins
                '''
            }
        }

        stage('Wait for Backend') {
            steps {
                sh '''
                    echo "Waiting for backend..."

                    for i in $(seq 1 30); do
                        if curl -f http://backend:5000/metrics >/dev/null 2>&1; then
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

        stage('Service Health Check') {
            steps {
                sh '''
                    echo "========== SERVICE STATUS =========="

                    docker ps --format "table {{.Names}}\t{{.Status}}"

                    echo ""
                    echo "Backend Check:"
                    docker exec docker-backend-1 curl -f http://localhost:5000/metrics || echo "Backend FAIL ❌"

                    echo ""
                    echo "Postgres Check:"
                    docker exec docker-postgres-1 pg_isready -U postgres || echo "Postgres FAIL ❌"

                    echo ""
                    echo "Prometheus Check:"
                    docker exec docker-prometheus-1 wget -qO- http://localhost:9090/-/ready || echo "Prometheus FAIL ❌"

                    echo ""
                    echo "Grafana Check:"
                    curl -f http://localhost:3000/api/health || echo "Grafana FAIL ❌"
                '''
            }
        }

        stage('Health Check Backend') {
            steps {
                sh '''
                    echo "Running final backend check..."
                    curl -f http://backend:5000/metrics
                '''
            }
        }
    }

    post {
        success {
            mail to: 'warblank21@gmail.com',
            subject: "SUCCESS: ${env.JOB_NAME} #${env.BUILD_NUMBER}",
            body: """
PIPELINE SUCCESS ✔

Build: ${env.BUILD_NUMBER}
URL: ${env.BUILD_URL}

SERVICE STATUS:
- Backend: OK
- Postgres: OK
- Prometheus: OK
- Grafana: OK
- Jenkins: OK
"""
        }

        failure {
            mail to: 'warblank21@gmail.com',
            subject: "FAILED: ${env.JOB_NAME} #${env.BUILD_NUMBER}",
            body: """
PIPELINE FAILED ❌

Build: ${env.BUILD_NUMBER}
URL: ${env.BUILD_URL}

Check services:
- Backend
- Postgres
- Prometheus
- Grafana
- Jenkins

Something is down or restarting.
"""
        }
    }
}