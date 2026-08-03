# Changelog

## [0.3.2](https://github.com/Mankeym/mankeym-auth/compare/v0.3.1...v0.3.2) (2026-08-03)


### 📚 Documentation

* complete changelog history ([b8d84de](https://github.com/Mankeym/mankeym-auth/commit/b8d84de2d137409401a1b7094d61f6ace15d9245))

## [0.3.1](https://github.com/Mankeym/mankeym-auth/compare/v0.3.0...v0.3.1) (2026-08-03)


### 🐛 Bug fixes

* **ci:** target repository for release auto-merge ([400f6df](https://github.com/Mankeym/mankeym-auth/commit/400f6df45e5e9fdebd86c36daacb77425a9d2fa3))


### 🤖 Release metadata

* publish v0.3.1 ([49e2c10](https://github.com/Mankeym/mankeym-auth/commit/49e2c10))

## [0.3.0](https://github.com/Mankeym/mankeym-auth/compare/v0.2.0...v0.3.0) (2026-08-03)


### ✨ Features

* **release:** document workflow verification ([a3ed6b8](https://github.com/Mankeym/mankeym-auth/commit/a3ed6b81bbff1ad935cca3ac9a2414d340fc66a0))


### 🤖 Release metadata

* merge release automation configuration ([b59a254](https://github.com/Mankeym/mankeym-auth/commit/b59a254b042d355ab29acf10caafb1108831bf78))
* publish v0.3.0 ([5ad013e](https://github.com/Mankeym/mankeym-auth/commit/5ad013e))

## [0.2.0](https://github.com/Mankeym/mankeym-auth/compare/v0.1.0...v0.2.0) (2026-08-03)


### ✨ Features

* **auth:** add role and permission management, security enhancements, and DB seeding ([385179a](https://github.com/Mankeym/mankeym-auth/commit/385179aa18d1f56161ae636151419d4963e51ab7))
* **auth:** harden authorization and session flows ([1bf9bec](https://github.com/Mankeym/mankeym-auth/commit/1bf9bec798447a9330e203de33fc7eec77a206e8))
* implement auth system, audit, docker infra, and Makefile ([f4cbc7f](https://github.com/Mankeym/mankeym-auth/commit/f4cbc7f40de4c2c8f29ed58b03a671d7e4402a0b))


### 🐛 Bug fixes

* add outbox leases and production safeguards ([634f998](https://github.com/Mankeym/mankeym-auth/commit/634f998a5d71f8a8ef499ee8d7d5d78bea559243))
* collect coverage once in ci ([042c16d](https://github.com/Mankeym/mankeym-auth/commit/042c16d5a0f343a92592e04e98d061138924fc47))
* configure smoke host before startup ([9428b71](https://github.com/Mankeym/mankeym-auth/commit/9428b71c02c427886adcdbde9d67f7c0adc6c368))
* configure smoke test host bootstrap ([5615716](https://github.com/Mankeym/mankeym-auth/commit/561571676b70d67bf5ae244594994e080fdb171f))
* configure testcontainers before api startup ([36c7432](https://github.com/Mankeym/mankeym-auth/commit/36c74329da55a81b8fbd47c943582409e6415935))
* fetch gitleaks push base in ci ([4ad1a62](https://github.com/Mankeym/mankeym-auth/commit/4ad1a627e9ad246dfdc29ad1d040a808748574e1))
* harden persistence and test startup ([3b459ac](https://github.com/Mankeym/mankeym-auth/commit/3b459ac9239bdae0c7a8a9b5c8086171f8ad19be))
* preserve jwt signing key across request scopes ([ffd36b3](https://github.com/Mankeym/mankeym-auth/commit/ffd36b39a9c73b8d5c1c2745eb11afe72c2ecfc9))
* remove lockfile-dependent dotnet cache ([f5ff6ba](https://github.com/Mankeym/mankeym-auth/commit/f5ff6ba5dfd9a37d54d73374bef607ce70716689))
* restrict trusted forwarded header sources ([e9eb3b7](https://github.com/Mankeym/mankeym-auth/commit/e9eb3b7a06b566eea6f9d5f563b39b054cbc6694))
* secure oauth callback token delivery ([6bf8477](https://github.com/Mankeym/mankeym-auth/commit/6bf8477cc8fa169e83df4cae77cf04d40602b484))
* tolerate duplicate coverage reports ([3449b05](https://github.com/Mankeym/mankeym-auth/commit/3449b055fbbbe188255896183960abaf9bc322db))


### 📚 Documentation

* add security operations and delivery guidance ([baa396d](https://github.com/Mankeym/mankeym-auth/commit/baa396db98c7dce7ef663812d2753d0892085a15))
* add project onboarding guide ([4bd3abd](https://github.com/Mankeym/mankeym-auth/commit/4bd3abdebacdef1f2710f22724772efb3deb8dfd))
* clarify project security posture ([96b9cd7](https://github.com/Mankeym/mankeym-auth/commit/96b9cd70ec8cb46754ac88e84ffdc67b93ad7f6f))
* add versioned API reference ([7d313f2](https://github.com/Mankeym/mankeym-auth/commit/7d313f20457a8c115561b62df38eadd5d63339b8))
* streamline project introduction ([0372b70](https://github.com/Mankeym/mankeym-auth/commit/0372b70f9f8442737544dda90571ac2564eea5e9))
* align documentation with production posture ([fcc5cc0](https://github.com/Mankeym/mankeym-auth/commit/fcc5cc07fa7fb84e9430dbd46da77bc8d6fc5194))
* add Postman API collection ([c32c649](https://github.com/Mankeym/mankeym-auth/commit/c32c649dbe843e070b5e91873a2899eba3116648))


### 👷 CI/CD

* enforce unit coverage baseline ([712c99a](https://github.com/Mankeym/mankeym-auth/commit/712c99a7e07ff05b71124c76d559a4d889f87c26))
* update Trivy Action reference ([75e6819](https://github.com/Mankeym/mankeym-auth/commit/75e68190ac41063299c5865499674f13b65b73f1))
* automate changelog releases ([483386d](https://github.com/Mankeym/mankeym-auth/commit/483386d0465147fde4600ab6f0236cbae539f30b))


### 🎨 Style

* format solution source files ([29a5ff5](https://github.com/Mankeym/mankeym-auth/commit/29a5ff586b3507032b664b3f9287a103d723e8a5))


### 🧹 Maintenance

* initialize repository ([b2c40ff](https://github.com/Mankeym/mankeym-auth/commit/b2c40ff71ab0602e8687ad4532c087fb7bdf16da))
* streamline local Docker workflow ([6a77de1](https://github.com/Mankeym/mankeym-auth/commit/6a77de1f24a2bfe7feff0520b65bbe579011fe96))
* remove generated artifacts and empty test project ([9296e14](https://github.com/Mankeym/mankeym-auth/commit/9296e141f5a60aaa6aa288c063c2aab00d977d44))
* tune analyzer policy and dispose JWT key ([bb36d68](https://github.com/Mankeym/mankeym-auth/commit/bb36d68eaacd9d0a913e9d73e6468a905e08005a))
* add pre-commit quality checks ([fc6c78e](https://github.com/Mankeym/mankeym-auth/commit/fc6c78e27ec1340e7a20eab381f43a9cb28b738d))
* enforce logging and coverage quality gates ([1ede631](https://github.com/Mankeym/mankeym-auth/commit/1ede631d77018e2ec5f74a5cfc8655e03520853f))
* remove default weather forecast request ([ea7abd3](https://github.com/Mankeym/mankeym-auth/commit/ea7abd34b8e45b883385c57d16357d93e177db68))


### 🤖 Release metadata

* publish v0.2.0 ([b391786](https://github.com/Mankeym/mankeym-auth/commit/b3917861a5f0805f8ac40e7bf9c00f51d5605337))
