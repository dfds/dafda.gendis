docker run -it --rm -p 5225:5225 \
    -e DB_CONNECTION_STRING="User ID=postgres;Password=p;Host=host.docker.internal;Port=5432;Database=dafdagendis;" \
    -e GENDIS_PREFIX_FOR_KAFKA="GENDIS_KAFKA_" \
    -e DAFDA_OUTBOX_NOTIFICATION_CHANNEL="dafda_outbox" \
    -e GENDIS_KAFKA_BOOTSTRAP_SERVERS="host.docker.internal:9092" \
    -e GENDIS_KAFKA_SSL_CA_LOCATION="" \
    dafda.gendis