import React from 'react';
import clsx from 'clsx';
import styles from './styles.module.css';

type FeatureItem = {
    title: string;
    // Svg: React.ComponentType<React.ComponentProps<'svg'>>;
    description: JSX.Element;
};

const FeatureList: FeatureItem[] = [
    {
        title: 'Projections',
        // Svg: require('@site/static/img/undraw_docusaurus_mountain.svg').default,
        description: (
            <>
                Projecting events to document and relational databases using ready-made components and building blocks.
                Use your own code and the Connector Sidecar to create new representations of your system state.
            </>
        ),
    },
    {
        title: 'Streaming',
        // Svg: require('@site/static/img/undraw_docusaurus_tree.svg').default,
        description: (
            <>
                Real-time message streaming from EventStoreDB to HTTP or gRPC endpoints or custom sinks.
            </>
        ),
    },
    {
        title: 'Observability',
        // Svg: require('@site/static/img/undraw_docusaurus_react.svg').default,
        description: (
            <>
                Top-down observability with OpenTelemetry built-in, with configurable exporters to Prometheus, OTLP protocol,
                Jaeger or Zipkin.
            </>
        ),
    },
];

function Feature({title, description}: FeatureItem) {
    return (
        <div className={clsx('col col--4')}>
            <div className="text--center">
                {/*<i className="fas fa-sitemap"/>*/}
                {/*<Svg className={styles.featureSvg} role="img" />*/}
            </div>
            <div className="text--center padding-horiz--md">
                <h3>{title}</h3>
                <p>{description}</p>
            </div>
        </div>
    );
}

export default function HomepageFeatures(): JSX.Element {
    return (
        <section className={styles.features}>
            <div className="container">
                <div className="row">
                    {FeatureList.map((props, idx) => (
                        <Feature key={idx} {...props} />
                    ))}
                </div>
            </div>
        </section>
    );
}
