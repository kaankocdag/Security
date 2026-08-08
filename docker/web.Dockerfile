FROM node:22-alpine AS deps
WORKDIR /app
COPY web/kaan-security-web/package.json web/kaan-security-web/package-lock.json* ./
RUN npm install --loglevel=error

FROM node:22-alpine AS build
WORKDIR /app
COPY --from=deps /app/node_modules ./node_modules
COPY web/kaan-security-web/ ./
ENV NEXT_TELEMETRY_DISABLED=1
RUN npm run build

FROM node:22-alpine AS runtime
WORKDIR /app
ENV NODE_ENV=production \
    NEXT_TELEMETRY_DISABLED=1 \
    PORT=3000

RUN addgroup -S nextjs && adduser -S nextjs -G nextjs
COPY --from=build /app/.next ./.next
COPY --from=build /app/public ./public
COPY --from=build /app/node_modules ./node_modules
COPY --from=build /app/package.json ./package.json
COPY --from=build /app/next.config.mjs ./next.config.mjs

USER nextjs
EXPOSE 3000
CMD ["npm", "run", "start"]
