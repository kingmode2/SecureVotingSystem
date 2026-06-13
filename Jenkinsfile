pipeline {
    agent any

    stages {

        stage('Docker Build') {
    steps {
        sh '''
            cd docker
            docker compose up -d --build
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