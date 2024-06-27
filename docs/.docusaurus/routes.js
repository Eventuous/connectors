import React from 'react';
import ComponentCreator from '@docusaurus/ComponentCreator';

export default [
  {
    path: '/__docusaurus/debug',
    component: ComponentCreator('/__docusaurus/debug', '5ff'),
    exact: true
  },
  {
    path: '/__docusaurus/debug/config',
    component: ComponentCreator('/__docusaurus/debug/config', '5ba'),
    exact: true
  },
  {
    path: '/__docusaurus/debug/content',
    component: ComponentCreator('/__docusaurus/debug/content', 'a2b'),
    exact: true
  },
  {
    path: '/__docusaurus/debug/globalData',
    component: ComponentCreator('/__docusaurus/debug/globalData', 'c3c'),
    exact: true
  },
  {
    path: '/__docusaurus/debug/metadata',
    component: ComponentCreator('/__docusaurus/debug/metadata', '156'),
    exact: true
  },
  {
    path: '/__docusaurus/debug/registry',
    component: ComponentCreator('/__docusaurus/debug/registry', '88c'),
    exact: true
  },
  {
    path: '/__docusaurus/debug/routes',
    component: ComponentCreator('/__docusaurus/debug/routes', '000'),
    exact: true
  },
  {
    path: '/docs',
    component: ComponentCreator('/docs', '986'),
    routes: [
      {
        path: '/docs',
        component: ComponentCreator('/docs', '9b9'),
        routes: [
          {
            path: '/docs',
            component: ComponentCreator('/docs', '2ac'),
            routes: [
              {
                path: '/docs/category/projectors',
                component: ComponentCreator('/docs/category/projectors', '655'),
                exact: true,
                sidebar: "defaultSidebar"
              },
              {
                path: '/docs/category/sinks',
                component: ComponentCreator('/docs/category/sinks', '58e'),
                exact: true,
                sidebar: "defaultSidebar"
              },
              {
                path: '/docs/custom-connector/',
                component: ComponentCreator('/docs/custom-connector/', 'b09'),
                exact: true,
                sidebar: "defaultSidebar"
              },
              {
                path: '/docs/deployment/',
                component: ComponentCreator('/docs/deployment/', '423'),
                exact: true,
                sidebar: "defaultSidebar"
              },
              {
                path: '/docs/intro',
                component: ComponentCreator('/docs/intro', '0d7'),
                exact: true,
                sidebar: "defaultSidebar"
              },
              {
                path: '/docs/projectors/grpc/',
                component: ComponentCreator('/docs/projectors/grpc/', 'd1e'),
                exact: true,
                sidebar: "defaultSidebar"
              },
              {
                path: '/docs/sinks/elasticsearch/',
                component: ComponentCreator('/docs/sinks/elasticsearch/', 'f8b'),
                exact: true,
                sidebar: "defaultSidebar"
              },
              {
                path: '/docs/sinks/grpc/',
                component: ComponentCreator('/docs/sinks/grpc/', 'e4e'),
                exact: true,
                sidebar: "defaultSidebar"
              },
              {
                path: '/docs/sinks/mongodb/',
                component: ComponentCreator('/docs/sinks/mongodb/', '08c'),
                exact: true,
                sidebar: "defaultSidebar"
              },
              {
                path: '/docs/sinks/sqlserver/',
                component: ComponentCreator('/docs/sinks/sqlserver/', '395'),
                exact: true,
                sidebar: "defaultSidebar"
              }
            ]
          }
        ]
      }
    ]
  },
  {
    path: '/',
    component: ComponentCreator('/', 'e5f'),
    exact: true
  },
  {
    path: '*',
    component: ComponentCreator('*'),
  },
];
