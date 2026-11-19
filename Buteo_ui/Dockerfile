# ---------- Stage 1: build the Angular app ----------
FROM node:24-alpine AS build
WORKDIR /app

# Install packages first (cached until package.json changes)
COPY package*.json ./
RUN npm install

# Copy the code and build static files into dist/bank-ui/browser
COPY . .
RUN npx ng build

# ---------- Stage 2: serve the files with nginx ----------
FROM nginx:alpine
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist/bank-ui/browser /usr/share/nginx/html
EXPOSE 80
