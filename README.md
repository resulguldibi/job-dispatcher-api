# netcore-api

 netcore-api contains example about consuming from kafka and producing to kafka using .net core.

- change directory to netcore-api project directory. <br/>
  cd "netcore-api"

- run build.sh. running build.sh starts operations below:
    - builds netcore-api docker image (docker build -t resulguldibi/netcore-api .)
    - starts netcore-api container and kafka container (docker-compose up -d)

- browse http://localhost:8080/index.html. send {"code":"connect","data":"{\"user\":\"resul\",\"ip\":\"1\"}"} data to register socket client.
- send message to socket client. ($ curl -kv http://localhost:8080/api/socket/messages -d '{"id":"resul|1","message":"test message"}' -H 'Content-Type: application/json')
- close socket message. ($ curl -kv http://localhost:8080/api/socket/close -d '{"id":"resul|1","message":"closed socket by server"}' -H 'Content-Type: application/json')



./kafka-topics.sh --zookeeper zookeper:2181 --topic SelectionSummary --partitions 1 --replication-factor 1 --create
./kafka-console-producer.sh --broker-list localhost:9092 --topic SelectionSummary


{"name":"xx","count":101,"user_id":"xxx-user"}