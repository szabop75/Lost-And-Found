# Frontend Dockerfile - Vite React build served by Nginx
FROM node:20-alpine AS build
WORKDIR /app

# Install deps
COPY frontend/package*.json ./
RUN npm ci

# Copy source and build
COPY frontend/ ./
# Allow overriding API base URL at build time
ARG VITE_API_BASE_URL
ENV VITE_API_BASE_URL=${VITE_API_BASE_URL}
RUN npm run build

# Nginx runtime
FROM nginx:1.27-alpine AS runtime
# Copy built assets
COPY --from=build /app/dist /usr/share/nginx/html
# Nginx config for SPA routing
COPY frontend/nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
