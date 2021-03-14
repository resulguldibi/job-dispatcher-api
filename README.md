# job-dispatcher-api

 job-dispatcher-api contains example about consuming from kafka and insert records to cassandra using .net core.

- change directory to job-dispatcher-api project directory. <br/>
  cd "job-dispatcher-api"

- run build.sh. running build.sh starts operations below:
    - builds job-dispatcher-api docker image (docker build -t resulguldibi/job-dispatcher-api .)
    - starts job-dispatcher-api,kafka, zookeeper and cassandra container (docker-compose up -d)