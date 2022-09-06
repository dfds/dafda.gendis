# dafda.gendis
---

TL;DR; `GenDis` is a generic out-of-process outbox dispatcher for dafda.

## How does it work?
It uses the `listen/notify` functionality in `postgres` that enables a dispatching application to be notified whenever a message is queued in a outbox.

## Where is it?
It is available as a `docker` container image on the official `docker` hub [https://hub.docker.com/r/dfdsdk/dafda-gendis](https://hub.docker.com/r/dfdsdk/dafda-gendis):

```shell
$ docker pull dfdsdk/dafda-gendis
```
