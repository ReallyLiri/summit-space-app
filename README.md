# SummIt

TBD

## Debug locally

Copy [SummIt/Properties/launchSettings.template.json](SummIt/Properties/launchSettings.template.json) to `SummIt/Properties/launchSettings.json` and fill in the missing app installation details.

Use `ngrok` to communicate between Space and your server, use the endpoint `https://<ngrok>/api/space`.

## Install to Space

### Install manually

Grant `Add custom emoji, View custom emoji` permissions in the **Global Authorization** settings.

### Install from link

Open your browser to

```sh
BASE_URL="https://<ngrok>"
https://jetbrains.com/space/app/install-app?name=SummIt&endpoint=$BASE_URL/api/space
```

## Run DB locally

Deployed app uses Heroku Postgres. To achieve same functionality locally we can use docker:

```sh
docker run --restart unless-stopped -d \
  --name postgres \
  -p 5432:5432 \
  -e POSTGRES_USER=summit \
  -e POSTGRES_PASSWORD=summit \
  -e POSTGRES_DB=summit \
  postgres:latest
```

## Run server with Docker

```sh
docker build -t summit .
docker run --rm -it -e PORT=5000 -p 5000:5000 --name summit summit
```