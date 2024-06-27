import type {Config} from "@docusaurus/types";
import type * as Preset from "@docusaurus/preset-classic";
import {themes} from "prism-react-renderer";

const config: Config = {
    title: 'Eventuous Connector',
    tagline: 'Connect your event-sourced apps to the outside world',
    favicon: 'img/favicon.ico',

    url: 'https://eventuous.dev',
    baseUrl: '/',

    organizationName: 'eventuous',
    projectName: 'connectors',

    onBrokenLinks: 'throw',
    onBrokenMarkdownLinks: 'warn',

    markdown: {
        mermaid: true,
    },
    themes: ['@docusaurus/theme-mermaid'],

    i18n: {
        defaultLocale: 'en',
        locales: ['en'],
    },

    presets: [[
        'classic',
        {
            docs: {
                // sidebarPath: require.resolve('./sidebars.ts'),
                editUrl: "https://github.com/eventuous/connectors/docs/edit/master"
            },
            blog: {
                showReadingTime: true,
            },
            theme: {
                customCss: require.resolve('./src/css/custom.css'),
            },
        } satisfies Preset.Options,
    ]],

    themeConfig: {
        metadata: [{name: 'keywords', content: 'event sourcing, eventsourcing, dotnet, .NET, .NET Core'}],
        image: 'img/social-card.png',
        // algolia: {
        //     appId: 'YQSSKN21VQ',
        //     apiKey: 'd62759f3b1948de19fea5476182dbd66',
        //     indexName: 'eventuous',
        // },
        navbar: {
            title: 'Eventuous Connector',
            logo: {
                alt: 'Eventuous logo',
                src: 'img/logo.png',
            },
            items: [
                {
                    type: 'doc',
                    docId: 'intro',
                    position: 'left',
                    label: 'Documentation',
                },
                {
                    href: 'https://github.com/sponsors/Eventuous',
                    position: 'right',
                    label: "Sponsor"
                },
                {
                    href: 'https://blog.eventuous.dev',
                    position: 'right',
                    label: "Blog"
                },
                {
                    href: 'https://github.com/eventuous/connectors',
                    position: 'right',
                    className: 'header-github-link',
                    'aria-label': 'GitHub repository',
                },
            ],
        },
        footer: {
            style: 'dark',
            links: [
                {
                    title: 'Docs',
                    items: [
                        {
                            label: 'Documentation',
                            to: 'docs/intro',
                        },
                        {
                            label: 'Framework',
                            href: "https://eventuous.dev"
                        },
                    ],
                },
                {
                    title: 'Community',
                    items: [
                        {
                            label: 'Discord',
                            href: 'https://discord.gg/ZrqM6vnnmf',
                        },
                    ],
                },
                {
                    title: 'More',
                    items: [
                        {
                            label: 'Blog',
                            href: 'https://blog.eventuous.dev',
                        },
                        {
                            label: 'GitHub',
                            href: 'https://github.com/eventuous/connectors',
                        },
                    ],
                },
            ],
            copyright: `Copyright © ${new Date().getFullYear()} Eventuous HQ OÜ. Built with Docusaurus.`,
        },
        prism: {
            theme: themes.vsLight,
            darkTheme: themes.vsDark,
            additionalLanguages: ['csharp'],
        },
    } satisfies Preset.ThemeConfig,
};

module.exports = config;
