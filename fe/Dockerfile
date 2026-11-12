# dr-photodrop-fe - immagine multi-stage: build Vite con Node, file statici serviti da nginx.
# nginx fa anche da proxy verso l'API (/api/): il browser non parla mai direttamente con il container API.

FROM node:24-alpine AS build
WORKDIR /app

# Prima i soli manifest: l'installazione resta in cache finche' le dipendenze non cambiano
COPY package.json package-lock.json ./
RUN npm ci

COPY . .
RUN npm run build

FROM nginx:1.28-alpine AS final

# Solo le variabili PHOTODROP_* vengono sostituite nel template: $uri e simili restano di nginx.
ENV NGINX_ENVSUBST_FILTER=^PHOTODROP_ \
    PHOTODROP_API_URL=http://dr-photodrop-api:8080

COPY nginx/default.conf.template /etc/nginx/templates/default.conf.template
COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 80

HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
  CMD wget -qO- http://127.0.0.1/healthz >/dev/null || exit 1
