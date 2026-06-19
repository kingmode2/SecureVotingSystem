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

    // ✅ FIX: post must be OUTSIDE stages
    post {
        success {
            mail to: 'warblank21@gmail.com',
            subject: "SUCCESS: ${env.JOB_NAME}",
            body: "Build succeeded!"
        }

        failure {
            mail to: 'warblank21@gmail.com',
            subject: "FAILED: ${env.JOB_NAME}",
            body: "Check Jenkins console output"
        }
    }
}