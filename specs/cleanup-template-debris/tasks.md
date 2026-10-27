# Tasks — cleanup-template-debris

- [ ] Frontend: reescribir `App.tsx` y `App.css` como página mínima del
      proyecto (sin contador, sin logos, sin assets demo).
- [ ] Frontend: eliminar `src/assets/hero.png`, `src/assets/react.svg`,
      `src/assets/vite.svg`, `public/icons.svg`.
- [ ] Frontend: actualizar `<title>` en `index.html`.
- [ ] CI: agregar `--collect:"XPlat Code Coverage"` al paso Test y subir el
      reporte como artifact.
- [ ] CI: quitar `continue-on-error: true` del paso "Check for vulnerable
      packages".
- [ ] Eliminar los 5 scripts one-off de la raíz (`scaffold-project.ps1`,
      `scaffold-health-check-tests.ps1`, `setup-github-repo.ps1`,
      `enable-ci-branch-protection.ps1`, `fix-openapi-vulnerability.ps1`).
- [ ] Verificación local: `dotnet build`, `dotnet test`, `npm run lint`,
      `npm run build` en verde.
- [ ] Abrir el PR desde `chore/29-cleanup-template-debris` hacia `main`.
