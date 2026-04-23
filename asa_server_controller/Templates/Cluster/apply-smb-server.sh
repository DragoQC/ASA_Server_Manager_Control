#!/usr/bin/env bash

set -euo pipefail

SERVER_CONFIG_PATH="${SERVER_CONFIG_PATH:-/opt/asa-control/smb/smb.conf}"
SYSTEM_SERVER_CONFIG_PATH="${SYSTEM_SERVER_CONFIG_PATH:-/etc/samba/smb.conf}"
SMB_SERVICE_NAME="${SMB_SERVICE_NAME:-smbd}"
SYSTEMCTL_BIN="${SYSTEMCTL_BIN:-/usr/bin/systemctl}"
TESTPARM_BIN="${TESTPARM_BIN:-/usr/bin/testparm}"

if [ "${EUID}" -ne 0 ]; then
  echo "This script must be run as root." >&2
  exit 1
fi

if [ ! -f "${SERVER_CONFIG_PATH}" ]; then
  echo "SMB server config not found: ${SERVER_CONFIG_PATH}" >&2
  exit 1
fi

mkdir -p "$(dirname "${SYSTEM_SERVER_CONFIG_PATH}")"

WAS_ACTIVE=0
if "${SYSTEMCTL_BIN}" is-active "${SMB_SERVICE_NAME}" --quiet; then
  WAS_ACTIVE=1
  "${SYSTEMCTL_BIN}" stop "${SMB_SERVICE_NAME}"
fi

install -m 0644 "${SERVER_CONFIG_PATH}" "${SYSTEM_SERVER_CONFIG_PATH}"
"${TESTPARM_BIN}" -s "${SYSTEM_SERVER_CONFIG_PATH}" >/dev/null
"${SYSTEMCTL_BIN}" enable "${SMB_SERVICE_NAME}"

if [ "${WAS_ACTIVE}" -eq 1 ]; then
  "${SYSTEMCTL_BIN}" start "${SMB_SERVICE_NAME}"
else
  "${SYSTEMCTL_BIN}" start "${SMB_SERVICE_NAME}"
fi

"${SYSTEMCTL_BIN}" status "${SMB_SERVICE_NAME}" --no-pager --full
