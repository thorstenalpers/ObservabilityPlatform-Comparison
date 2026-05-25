
helm repo add signoz https://charts.signoz.io
helm repo update

helm uninstall signoz
helm install signoz signoz/signoz

pause