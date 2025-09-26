# Branding assets for printouts

Place your organization logo here to be shown on all generated PDFs.

- Expected file: `logo.png` (recommended: transparent background, ~800x800px or vector SVG if supported)
- Configure in `appsettings.json` under the `Branding` section:
  - `"Branding": {
       "LogoPath": "branding/logo.png",
       "DepositLogoPath": "branding/logo.png",
       "OrganizationName": "<Szervezet neve>"
     }`

Notes
- Paths are relative to the API content root (this folder lives in `backend/LostAndFound.Api/branding/`).
- If `DepositLogoPath` is not set, the service tries a fallback search, but for consistency set both to the same file.
- You can also set different logos per document type by pointing to different files.
