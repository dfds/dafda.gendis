FROM mcr.microsoft.com/dotnet/aspnet:6.0-alpine

WORKDIR /app

# ADD Curl
RUN apk update && apk add curl && apk add ca-certificates && rm -rf /var/cache/apk/*

# AWS RDS Certificate
RUN curl -o /tmp/rds-combined-ca-bundle.pem https://truststore.pki.rds.amazonaws.com/global/global-bundle.pem \
    && mv /tmp/rds-combined-ca-bundle.pem /usr/local/share/ca-certificates/rds-combined-ca-bundle.crt \
    && update-ca-certificates

# OpenSSL cert for Kafka
RUN curl -sS -o /app/cert.pem https://curl.se/ca/cacert.pem
ENV GENDIS_KAFKA_SSL_CA_LOCATION=/app/cert.pem

# create and use non-root user
RUN adduser \
  --disabled-password \
  --home /app \
  --gecos '' app \
  && chown -R app /app
USER app

ENV DOTNET_RUNNING_IN_CONTAINER=true \
  ASPNETCORE_URLS=http://+:5225

COPY ./.output/app ./

EXPOSE 5225

ENTRYPOINT [ "dotnet", "Dafda.Gendis.App.dll" ]