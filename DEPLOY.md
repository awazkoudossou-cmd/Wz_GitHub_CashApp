# Déploiement sur Render (backend + frontend + PostgreSQL)

Ce projet est prêt à être déployé entièrement sur [Render](https://render.com) :
- **cashapp-api** — Web Service Docker (.NET 8)
- **cashapp-web** — Static Site (React/Vite)
- **cashapp-db** — PostgreSQL managé

Un fichier [`render.yaml`](render.yaml) (Blueprint Render) décrit déjà les 3 ressources.

## 1. Créer un compte Render

Va sur https://render.com et crée un compte (gratuit pour démarrer). Connecte-le à ton
compte GitHub pour qu'il puisse lire le dépôt `Wz_GitHub_CashApp`.

## 2. Déployer via Blueprint

1. Dans le dashboard Render : **New +** → **Blueprint**
2. Sélectionne le dépôt `awazkoudossou-cmd/Wz_GitHub_CashApp`
3. Render détecte `render.yaml` et propose de créer les 3 ressources (`cashapp-db`,
   `cashapp-api`, `cashapp-web`) — clique sur **Apply**
4. Le premier déploiement prend quelques minutes (build Docker du backend + build Vite
   du frontend + provisioning Postgres)

## 3. Connecter les deux services entre eux (étape manuelle, une seule fois)

Le `render.yaml` ne peut pas connaître à l'avance les URLs `*.onrender.com` générées.
Une fois les deux services créés :

1. Note l'URL de **cashapp-web** (ex: `https://cashapp-web.onrender.com`)
2. Note l'URL de **cashapp-api** (ex: `https://cashapp-api.onrender.com`)
3. Dans **cashapp-api → Environment**, mets à jour :
   - `Cors__AllowedOrigins__0` = URL de cashapp-web (ex: `https://cashapp-web.onrender.com`)
4. Dans **cashapp-web → Environment**, mets à jour :
   - `VITE_API_BASE_URL` = URL de cashapp-api (ex: `https://cashapp-api.onrender.com`)
5. Redéploie manuellement les deux services (**Manual Deploy** → **Deploy latest commit**)
   pour que les nouvelles variables d'environnement soient prises en compte
   (`VITE_API_BASE_URL` est injectée au *build* du frontend, pas au runtime).

## 4. Premier login

Au premier démarrage, le backend crée le schéma PostgreSQL (`EnsureCreatedAsync`) et
seed le compte admin — mêmes identifiants qu'en local :

```
Utilisateur : admin
Mot de passe : Admin@123
```

**Change ce mot de passe une fois connecté** (Utilisateurs → admin), vu que l'app sera
publique.

## 5. Vérifications post-déploiement

- `https://<cashapp-api>.onrender.com/swagger` doit répondre (Swagger UI)
- Se connecter sur `https://<cashapp-web>.onrender.com` avec admin/Admin@123
- Vérifier dans les DevTools réseau qu'il n'y a pas d'erreur CORS ni 401 sur `/api/...`

## Notes techniques

- Le plan **free** de Render met les services en veille après 15 min d'inactivité
  (le premier chargement après une pause peut prendre ~30-60s le temps que le
  conteneur redémarre). Passe sur un plan payant pour éviter ça en production réelle.
- La base PostgreSQL du plan free expire **30 jours** après sa création si tu ne
  passes pas sur une instance payante (à partir de 6 $/mois pour la plus petite,
  Basic-256mb) — pense à surveiller ça si tu restes en gratuit.
- Le backend détecte automatiquement PostgreSQL vs SQLite selon le format de la
  chaîne de connexion (`Host=...` ou `postgres://...` → PostgreSQL ; `Data Source=...`
  → SQLite). Le développement local avec `start-backend.ps1` continue donc à utiliser
  SQLite sans rien changer.
- Je n'ai pas pu tester le build Docker localement (Docker non installé sur cette
  machine) — le premier déploiement Render fera donc office de premier test réel du
  `Dockerfile`. Si le build échoue, regarde les logs de build dans le dashboard Render.
