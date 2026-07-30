FROM node:22-alpine AS build
WORKDIR /src

COPY package.json package-lock.json ./
RUN npm ci

COPY . .
RUN npm run build

FROM node:22-alpine AS runtime
WORKDIR /app
COPY --from=build /src/build ./build

EXPOSE 3000
ENV PORT=3000
ENTRYPOINT ["node", "build/index.js"]
