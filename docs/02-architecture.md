# Import Module architecture

## Technologies Stack Used

- **.NET Core** and **ASP.NET Core** as base platform
- **EF Core** as primary ORM
- **ASP.NET Core Identity** for authentication and authorization
- **OpenIddict** for OAuth authentication
- **WebPack** as primary design/runtime bundler and minifier
- **SignalR Core** for push notifications
- **Vue3** Progressive frontend framework with its key features allows to build fast applications
- **Typescript** for working code of Import App
- **TailwindCSS** as primary CSS framework providing wonderful flexible structure to speed up styling
- **Yarn** to build Import App

## Module folder structure

```text
?? docs                                      // module documentation
?? samples                                   // helpful to start develop your own Importers
?? src                                       // the main codebase of project
?  ?? VirtoCommerce.ImportModule.Core        // core models and definitions
?  ?? VirtoCommerce.ImportModule.CsvHelper   // import realisation for CSV format. Can be used like sample in development
?  ?? VirtoCommerce.ImportModule.Data        // main services and models to work with data
?  ?? VirtoCommerce.ImportModule.Data.***    // data proveder-oriented adapters (supports MySql, PostgreSql and SqlServer)
?  ?? VirtoCommerce.ImportModule.Web         // controllers and frontend part of project
?  ?  ??import-app                           // source code of Import App
?  ?  ??...
?? tests                                     // unit and functional tests
```
## Import App folder structure

```text
?? public                                    // static assets, images, etc
?? src
?  ?? api_client                             // generated API clients folder
?  ?? composables                            // application composables
?  ?? locales                                // locale files used to provide translated content
?  ?? modules                                // the collection of custom modules
?  ?  ?? ...                                 // module folder
?  ?     ?? components                       // components specific for this module
?  ?     ?? composables                      // shared logic written using Composable API pattern.
?  ?     ?? locales                          // locale files used to provide translated content specific for this module
?  ?     ?? pages                            // set of module pages used within Application router
?  ?     ?? index.ts                         // module entry point
?  ?? pages                                  // set of application pages used within Application router.
?  ?? router                                 // SPA routing configuration
?  ?? styles                                 // extras application style files
?  ?? types                                  // typescript .d.ts files
```

## Getting started Import App

```bash
# install and configure package dependencies and git hooks
$ yarn

# build application
$ yarn build

# start application with hot reload at localhost:8080
$ yarn serve
```

###  Import App Version bumping

```bash
$ yarn bump patch/minor/major
```
