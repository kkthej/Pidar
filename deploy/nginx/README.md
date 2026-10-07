# "We'll be right back" page

nginx (on the host, in front of the PIDAR containers) shows `pidar-maintenance.html`
instead of its plain "502 Bad Gateway" whenever the app does not answer: during
`docker restart PidarWeb`, a rebuild, or an outage. The page checks every 15 seconds
and reloads itself as soon as PIDAR is back. It is self-contained (logo embedded),
so it works even when the app is completely down.

## Install (once, on the server, as root)

```bash
# 1. put the page where nginx can read it
mkdir -p /var/www/pidar-errors
cp /home/pidar/deploy/nginx/pidar-maintenance.html /var/www/pidar-errors/

# 2. find the nginx site file for pidar.hpc4ai.unito.it and back it up
grep -rl "pidar.hpc4ai" /etc/nginx/sites-enabled/ /etc/nginx/conf.d/ 2>/dev/null
cp /etc/nginx/sites-enabled/<that-file> /root/<that-file>.bak
```

3. In that file, inside the `server { ... }` block that has `server_name pidar.hpc4ai.unito.it`
   and `listen 443`, add (next to the other `location` blocks):

```nginx
    # PIDAR: friendly page while the app is restarting or down
    error_page 502 503 504 /pidar-maintenance.html;
    location = /pidar-maintenance.html {
        root /var/www/pidar-errors;
        internal;
        add_header Cache-Control "no-store" always;
    }
```

```bash
# 4. check and apply (no downtime)
nginx -t && systemctl reload nginx
```

## Updating the page later

After a `git pull` that changes `deploy/nginx/pidar-maintenance.html`, copy it again (step 1).

## Test

`docker stop PidarWeb`, open the site (the page appears), then `docker start PidarWeb`:
within about a minute the page reloads into PIDAR by itself.
