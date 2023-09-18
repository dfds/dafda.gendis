FROM mcr.microsoft.com/dotnet/sdk:6.0 as builder

COPY src /src
RUN apt update && apt install -y librdkafka-dev
RUN cd /src/Dafda.Gendis.App && dotnet publish -c Release -o /build/out

FROM mcr.microsoft.com/dotnet/aspnet:6.0

WORKDIR /app

# ADD Curl
RUN apt update && apt install -y curl librdkafka-dev ca-certificates && rm -rf /var/cache/apk/*

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

COPY --from=builder /build/out/ ./

EXPOSE 5225

ENTRYPOINT [ "dotnet", "Dafda.Gendis.App.dll" ]