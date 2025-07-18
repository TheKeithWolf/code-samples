using Amazon.CDK;
using Amazon.CDK.AWS.ECR;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.Logs;
using Constructs;

namespace PayPalWithdrawalService.Cdk
{
    public class PayPalWithdrawalServiceStack : Stack
    {
        internal PayPalWithdrawalServiceStack(Construct scope, string id, IStackProps props = null): base(scope, id, props)
        {
            var ecrRepository = new Repository(this, "PayPalWithdrawalServiceRepo", new RepositoryProps
            {
                RepositoryName = "paypal-withdrawal-service",
                LifecycleRules = new ILifecycleRule[]
                {
                    new LifecycleRule
                    {
                        TagStatus = TagStatus.ANY,
                        MaxImageCount = 5,
                        Description = "Keep last 5 images"
                    }
                }
            });

            var vpc = new Vpc(this, "PayPalWithdrawalServiceVPC", new VpcProps
            {
                MaxAzs = 2 
            });

            var cluster = new Cluster(this, "PayPalWithdrawalServiceCluster", new ClusterProps
            {
                Vpc = vpc,
                ClusterName = "PayPalWithdrawalServiceCluster"
            });

            var taskDefinition = new FargateTaskDefinition(this, "PayPalWithdrawalServiceTaskDef", new FargateTaskDefinitionProps
            {
                MemoryLimitMiB = 512, 
                Cpu = 256,            
                TaskRole = new Role(this, "PayPalWithdrawalServiceTaskRole", new RoleProps
                {
                    AssumedBy = new ServicePrincipal("ecs-tasks.amazonaws.com"),
                    Description = "IAM role for ECS tasks of PayPal Withdrawal Service",
                    ManagedPolicies = new IManagedPolicy[]
                    {
                        ManagedPolicy.FromAwsManagedPolicyName("service-role/AmazonECSTaskExecutionRolePolicy") 
                    }
                }),
                ExecutionRole = new Role(this, "PayPalWithdrawalServiceExecutionRole", new RoleProps
                {
                    AssumedBy = new ServicePrincipal("ecs-tasks.amazonaws.com"),
                    Description = "IAM role for ECS task execution of PayPal Withdrawal Service",
                    ManagedPolicies = new IManagedPolicy[]
                    {
                        ManagedPolicy.FromAwsManagedPolicyName("service-role/AmazonECSTaskExecutionRolePolicy") 
                    }
                })
            });

            var appContainer = taskDefinition.AddContainer("PayPalWithdrawalServiceContainer", new ContainerDefinitionOptions
            {
                Image = ContainerImage.FromEcrRepository(ecrRepository, "latest"), 
                Logging = new AwsLogDriver(new AwsLogDriverProps
                {
                    StreamPrefix = "paypal-withdrawal-service",
                    LogGroup = new LogGroup(this, "PayPalWithdrawalServiceLogGroup", new LogGroupProps
                    {
                        LogGroupName = "/ecs/paypal-withdrawal-service",
                        Retention = RetentionDays.ONE_WEEK 
                    })
                }),

            });

            var fargateService = new FargateService(this, "PayPalWithdrawalServiceFargateService", new FargateServiceProps
            {
                Cluster = cluster,
                TaskDefinition = taskDefinition,
                DesiredCount = 1, 
                AssignPublicIp = true, 
                VpcSubnets = new SubnetSelection { SubnetType = SubnetType.PUBLIC }, 
                ServiceName = "paypal-withdrawal-service"
            });

            new CfnOutput(this, "ECRRepositoryUri", new CfnOutputProps
            {
                Value = ecrRepository.RepositoryUri,
                Description = "URI of the ECR repository"
            });

            new CfnOutput(this, "ECSClusterName", new CfnOutputProps
            {
                Value = cluster.ClusterName,
                Description = "Name of the ECS cluster"
            });

            new CfnOutput(this, "ECSServiceName", new CfnOutputProps
            {
                Value = fargateService.ServiceName,
                Description = "Name of the ECS service"
            });
        }
    }
}