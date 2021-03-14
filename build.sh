docker build -t resulguldibi/job-dispatcher-api .
docker-compose up -d


#create topic in kafka
docker exec -it kafka /opt/kafka/bin/kafka-topics.sh --zookeeper zookeeper:2181 --topic Test_Records --partitions 1 --replication-factor 1 --create

#decribe kafka topic
docker exec -it kafka /opt/kafka/bin/kafka-topics.sh --describe --zookeeper zookeeper:2181 --topic Test_Records

#alter kafka topic to add new partition (set partition count to 2)
docker exec -it kafka /opt/kafka/bin/kafka-topics.sh --zookeeper zookeeper:2181 --alter --topic Test_Records --partitions 2 
   
#create keyspace and table in cassandra. insert sample record to cassandra table.
docker exec -it cassandra cqlsh -e "CREATE KEYSPACE my_keyspace WITH replication = {'class' : 'NetworkTopologyStrategy','my_datacenter' : 1};"
docker exec -it cassandra cqlsh -e "CREATE TABLE my_keyspace.test_records (id text, name text, PRIMARY KEY (id, name));"
docker exec -it cassandra cqlsh -e "CREATE INDEX on my_keyspace.test_records(name);"
