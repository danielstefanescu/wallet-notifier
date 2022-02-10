# About
An application that looks for changes in one or more Cardano addresses.

# Prerequisites
- www.blockfrost.io Api key
- SMTP account

# Usage
- clone the repo
- update the appsettings.json file with your own values:
  - cron: cron expression for the scheduled job
  - blockfrostio: see https://docs.blockfrost.io/#section/Authentication
  - wallets: list of stake addresses
  - MailSettings: settings for sending the email notification
- build the Docker image
- start the container
