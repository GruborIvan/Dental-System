# Deploying Ordinacija to the clinic

The clinic gets different connection settings than your dev machine, and the password must never be in git.
Two untracked files hold the real values; the publish script picks the right one and checks the result.

| File (in `Ordinacija/`) | Used for | In git? |
|---|---|---|
| `appsettings.json` | placeholders only | yes |
| `appsettings.Local.json` | **your dev** server, when you run/debug | no (ignored) |
| `appsettings.Deploy.json` | **the clinic** server, only when publishing | no (ignored) |

At runtime the app reads `appsettings.json`, then lets `appsettings.Local.json` override it.
A clinic package contains `appsettings.Deploy.json` renamed to `appsettings.Local.json`.
The dev file is never packaged.

## One-time setup on a dev machine

1. Create `Ordinacija/appsettings.Local.json` with your dev connection string(s).
2. Create `Ordinacija/appsettings.Deploy.json`:

   ```json
   {
     "ConnectionStrings": {
       "PantheonDB": "Data Source=<clinic server>;Initial Catalog=Ordinacija;User Id=<user>;Password=<password>;MultipleActiveResultSets=true;TrustServerCertificate=True;Integrated Security=False;"
     }
   }
   ```

   The key must be `PantheonDB` (the only one the code reads). Keep the password in your password manager as well:
   these files are not in git, so a lost disk means a lost password.
3. Visual Studio 2022 must be installed (ClickOnce needs its MSBuild to build `setup.exe`).

## Every release

1. Finish the work and **commit it** (the script refuses to publish with uncommitted changes, so a package can always be rebuilt from a commit).
2. From the repo root:

   ```powershell
   .\Deploy\Publish-Ordinacija.ps1                # ClickOnce -> C:\OrdinacijaDeploy\ClickOnce\
   .\Deploy\Publish-Ordinacija.ps1 -Mode Folder   # plain folder -> C:\OrdinacijaDeploy\
   ```

3. The script prints the version, the commit and the **server the package will use**, and then checks the
   published files: clinic file present, no placeholders, a password set, server different from your dev server,
   `appsettings.json` free of passwords. It never prints the password. If any check fails it stops with an error:
   do not install that package.
4. ClickOnce only: the script raised `ApplicationRevision` in `Properties/PublishProfiles/Clinic-ClickOnce.pubxml`.
   Commit that change; it is the record of which version was published from which commit.
5. Install:
   - **ClickOnce:** run `C:\OrdinacijaDeploy\ClickOnce\setup.exe` on each clinic PC. Updates are switched off in the profile,
     so PCs do not update themselves; run the new `setup.exe` on each one.
   - **Folder:** copy the folder to the clinic PC (needs the .NET 8 Desktop Runtime installed there).
6. Smoke test on a clinic PC: log in, the patient list loads, save a patient, print a report.
   Errors are also written to `%LocalAppData%\Ordinacija\errors.log`.

## Good to know

- **Test without touching the clinic folder:** `-OutputDir C:\Temp\ordinacija-test` (add `-AllowDirty` if you have uncommitted changes).
  A test publish does not change the revision number.
- **Stale files in Folder mode:** a folder publish only overwrites. Files from older builds stay (for example the old
  `System.Data.SqlClient.dll`). For a clean copy, publish with `-OutputDir` into an empty folder and copy that.
- **Visual Studio's Publish button** works too if you pick `Clinic-ClickOnce` or `Clinic-Folder`; the same guard runs
  during the build. It does not run the package check, so prefer the script.
- **Old profiles** (`ClickOnceProfile*.pubxml`, `FolderProfile*.pubxml`, untracked) do not select the clinic file.
  A package made with them only contains the placeholder `appsettings.json` and cannot connect. That is intentional.
- **Guard errors** come from `ValidateDeploySettings` in `Ordinacija.csproj`: missing file, placeholder values,
  or the same server as your dev file.
- **Changing the password:** update `appsettings.Deploy.json` and `appsettings.Local.json`, publish again and
  reinstall on the clinic PCs.
- **The package still contains the password** in a readable file on each clinic PC. Using a SQL login that can only
  access the Ordinacija database (instead of `sa`) limits the damage.
