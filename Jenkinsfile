pipeline {
    agent any

    stages {

        stage('Deploy Containers (Clean Start)') {
            steps {
                sh '''
                    cd docker
                    docker compose up -d backend frontend postgres pgadmin prometheus grafana
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

        stage('Health Check Backend') {
            steps {
                sh '''
                    echo "Running final health check..."
                    curl -f http://backend:5000/metrics
                '''
            }
        }
    }

    post {
        success {
            emailext(
                to: 'warblank21@gmail.com',
                subject: "SUCCESS: ${env.JOB_NAME} - Build #${env.BUILD_NUMBER}",
                body: """
✔ BUILD SUCCESS

Project: ${env.JOB_NAME}
Build Number: ${env.BUILD_NUMBER}

STAGES:
✔ Deploy Containers
✔ Wait for Backend
✔ Health Check Backend

Backend Status: RUNNING ✔
Metrics: OK ✔

Jenkins URL:
${env.BUILD_URL}
                """
            )
        }

        failure {
            emailext(
                to: 'warblank21@gmail.com',
                subject: "FAILED: ${env.JOB_NAME} - Build #${env.BUILD_NUMBER}",
                body: """
❌ BUILD FAILED

Project: ${env.JOB_NAME}
Build Number: ${env.BUILD_NUMBER}

Check which stage failed in Jenkins:
- Deploy Containers
- Wait for Backend
- Health Check Backend

Jenkins URL:
${env.BUILD_URL}
                """
            )
        }
    }
}