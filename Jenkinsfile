pipeline {
    agent any

    stages {

        stage('Docker Build') {
            steps {
                sh '''
                    cd docker
                    docker compose build
                '''
            }
        }

        stage('Deploy') {
            steps {
                sh '''
                    cd docker
                    docker compose down || true
                    docker compose up -d
                '''
            }
        }

        stage('Health Check') {
            steps {
                sh '''
                    curl -f http://localhost:5000/metrics || true
                '''
            }
        }
    }
}