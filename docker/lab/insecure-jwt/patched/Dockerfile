FROM python:3.12-alpine
RUN adduser -D -u 65534 labuser
WORKDIR /app
COPY server.py .
USER 65534:65534
EXPOSE 8080
CMD ["python", "-u", "server.py"]
