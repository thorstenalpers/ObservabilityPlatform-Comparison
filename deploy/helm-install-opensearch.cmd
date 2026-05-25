

REM helm repo add open-telemetry https://open-telemetry.github.io/opentelemetry-helm-charts
REM helm repo add opensearch-project-helm-charts https://opensearch-project.github.io/helm-charts
REM helm repo update

helm uninstall data-prepper
helm uninstall opentelemetry-collector 

helm uninstall opensearch
helm uninstall opensearch-dashboard

helm install opensearch opensearch-project-helm-charts/opensearch --version 3.6.0 -f ./values.opensearch.yaml --wait
helm install opensearch-dashboard opensearch-project-helm-charts/opensearch-dashboards --version 3.6.0 -f ./values.opensearch-dashboards.yaml --wait

helm install data-prepper opensearch-project-helm-charts/data-prepper --version 0.3.1 -f ./values.data-prepper.yaml --wait
helm install opentelemetry-collector open-telemetry/opentelemetry-collector -f ./values.opentelemetry-collector.yaml --wait

REM helm show values open-telemetry/opentelemetry-collector  --version 0.156.2 > values.opentelemetry-collector.yaml


REM helm show values opensearch-project-helm-charts/data-prepper --version 0.3.1 > values.data-prepper.yaml

REM pause