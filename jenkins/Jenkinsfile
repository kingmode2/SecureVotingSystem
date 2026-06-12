pipeline {
    agent any

    stages {

        stage('Checkout') {
            steps {
                git url: 'https://github.com/kingmode2/SecureVotingSystem.git', branch: 'main'
            }
        }

        stage('Build Backend') {
            steps {
                sh '''
                    cd backend/SecureVotingSystem
                    dotnet restore
                    dotnet build -c Release
                '''
            }
        }

        stage('Build Frontend') {
            steps {
                sh '''
                    cd frontend
                    npm ci
                    npm run build
                '''
            }
        }

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