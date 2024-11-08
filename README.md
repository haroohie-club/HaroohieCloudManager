# Haroohie Cloud Manager
This is a simple CLI that is used for managing various "cloud" operations in our CI builds.

Specifically it can:
* Post patches (and other files) to Digital Ocean storage (and send a Discord message linking to them)
* Check if a CORS proxy is up (and send a Discord message if it's not)
* Commit and push Weblate projects
* Download a ROM (or other file) from Digital Ocean storage (and unzip it if it's zipped)